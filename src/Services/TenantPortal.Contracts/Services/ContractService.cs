using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using TenantPortal.Contracts.Data;
using TenantPortal.Contracts.DTOs;
using TenantPortal.Contracts.Interfaces;
using TenantPortal.Contracts.Models;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Contracts.Services
{
    public class ContractService : IContractService
    {
        private readonly ContractDbContext _context;
        private readonly BlobServiceClient _blobClient;
        public ContractService(ContractDbContext context, BlobServiceClient blobClient)
        {
            _context = context;
            _blobClient = blobClient;
        }
        public async Task<bool> DeleteContractAsync(Guid contractId, Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant)
            {
                return false;
            }
            try
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted);
                if (contract == null) return false;
                contract.IsDeleted = true;
                contract.DeletedAt = DateTime.UtcNow;
                contract.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        public async Task<ContractDownloadResponseDTO?> DownloadContractAsync(Guid contractId, Guid userId, UserRole role)
        {
            Contract? contract;
            if (role == UserRole.Tenant)
                contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == userId && !c.IsDeleted);
            else if (role == UserRole.Admin)
                contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && c.UploadedBy == userId && !c.IsDeleted);
            else
                contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted);

            if (contract == null) return null;

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
            var downloadUrl = GenerateSasUrl(contract.BlobStoragePath, expiresAt);

            return new ContractDownloadResponseDTO
            {
                FileName = contract.FileName,
                DownloadUrl = downloadUrl,
                ExpiresAt = expiresAt.UtcDateTime
            };
        }

        public async Task<List<ContractResponseDTO>> GetAllContractsAsync(Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant)
            {
                var contracts = await _context.Contracts
                    .Where(c => c.TenantId == userId && !c.IsDeleted)
                    .ToListAsync();
                return contracts.Select(MapToDTO).ToList();
            }

            if (role == UserRole.Admin)
            {
                // Admins see only contracts they uploaded (i.e. contracts for their own tenants).
                // UploadedBy is always the uploading Admin, so this correctly scopes to their tenant portfolio.
                var contracts = await _context.Contracts
                    .Where(c => c.UploadedBy == userId && !c.IsDeleted)
                    .ToListAsync();
                return contracts.Select(MapToDTO).ToList();
            }

            // SuperAdmin sees everything
            return (await _context.Contracts.Where(c => !c.IsDeleted).ToListAsync())
                .Select(MapToDTO).ToList();
        }

        public async Task<ContractResponseDTO?> GetContractAsync(Guid contractId, Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant)
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c =>
                    c.Id == contractId && c.TenantId == userId && !c.IsDeleted);
                return contract == null ? null : MapToDTO(contract);
            }

            if (role == UserRole.Admin)
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c =>
                    c.Id == contractId && c.UploadedBy == userId && !c.IsDeleted);
                return contract == null ? null : MapToDTO(contract);
            }

            // SuperAdmin can access any contract
            var adminContract = await _context.Contracts.FirstOrDefaultAsync(c =>
                c.Id == contractId && !c.IsDeleted);
            return adminContract == null ? null : MapToDTO(adminContract);
        }

        public async Task<bool> UploadContractAsync(UploadContractRequestDTO request, Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant)
            {
                return false;
            }
            try
            {    Guid id = Guid.NewGuid();
                Contract contract = new Contract
                {
                    Id = id,
                    TenantId = request.TenantId,
                    UnitId = request.UnitId,
                    FileName = request.File.FileName,
                    BlobStoragePath = $"contracts/{request.TenantId}/{request.UnitId}/{id}",
                    IsCurrent = true,
                    IsDeleted = false,
                    UploadedBy = userId,
                    UploadedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var contracts = await _context.Contracts.Where(c => c.TenantId == request.TenantId && c.UnitId == request.UnitId && !c.IsDeleted && c.IsCurrent).ToListAsync();
                foreach (var c in contracts)
                {
                    c.IsCurrent = false;
                    c.UpdatedAt = DateTime.UtcNow;
                }
                await _context.Contracts.AddAsync(contract);
                await _blobClient.GetBlobContainerClient("contracts").GetBlobClient(contract.BlobStoragePath).UploadAsync(request.File.OpenReadStream());
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        private ContractResponseDTO MapToDTO(Contract contract) => new ContractResponseDTO
        {
            Id = contract.Id,
            TenantId = contract.TenantId,
            FileName = contract.FileName,
            IsCurrent = contract.IsCurrent,
            UploadedAt = contract.UploadedAt,
            // Listing URLs use a longer expiry (1 h) so the page remains usable without re-fetching.
            // Actual download URLs (15 min) are issued by DownloadContractAsync.
            DownloadUrl = GenerateSasUrl(contract.BlobStoragePath, DateTimeOffset.UtcNow.AddHours(1))
        };

        /// <summary>
        /// Generates a time-limited Azure Blob SAS read URL.
        /// Falls back to an empty string when the BlobServiceClient lacks shared-key credentials
        /// (e.g. managed identity without user-delegation key setup).
        /// </summary>
        private string GenerateSasUrl(string blobPath, DateTimeOffset expiresOn)
        {
            try
            {
                var blobClient = _blobClient
                    .GetBlobContainerClient("contracts")
                    .GetBlobClient(blobPath);
                return blobClient.GenerateSasUri(
                    Azure.Storage.Sas.BlobSasPermissions.Read,
                    expiresOn).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
