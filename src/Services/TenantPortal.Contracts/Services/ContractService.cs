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
            Contract? contract = null;
            if (role == UserRole.Tenant)
            {
                contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == userId && !c.IsDeleted);
            }
            else
            {
                contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted);
            }
            if (contract == null) return null;
            var response = new ContractDownloadResponseDTO
            {
                FileName = contract.FileName,
                DownloadUrl = "TEST",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
            return response;
        }

        public async Task<List<ContractResponseDTO>> GetAllContractsAsync(Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant)
            {
                var contracts = await _context.Contracts.Where(c => c.TenantId == userId && !c.IsDeleted).ToListAsync();
                if (contracts == null || !contracts.Any()) return new List<ContractResponseDTO>();
                List<ContractResponseDTO> response = new List<ContractResponseDTO>();
                foreach (var contract in contracts)
                {
                    response.Add(MapToDTO(contract));
                }
                return response;
            }
            var allContracts = await _context.Contracts.Where(c => !c.IsDeleted).ToListAsync();
            if (allContracts == null || !allContracts.Any()) return new List<ContractResponseDTO>();
            List<ContractResponseDTO> allResponse = new List<ContractResponseDTO>();
            foreach (var contract in allContracts)
            {
                allResponse.Add(MapToDTO(contract));
            }
            return allResponse;
        }

        public async Task<ContractResponseDTO?> GetContractAsync(Guid contractId, Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant)
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == userId && !c.IsDeleted);
                if (contract == null) return null;
                return MapToDTO(contract);
            }
            var contractForAdmin = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted);
            if (contractForAdmin == null) return null;
            return MapToDTO(contractForAdmin);
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

        private ContractResponseDTO MapToDTO(Contract contract)
        {
            return new ContractResponseDTO
            {
                Id = contract.Id,
                TenantId = contract.TenantId,
                FileName = contract.FileName,
                IsCurrent = contract.IsCurrent,
                UploadedAt = contract.UploadedAt,
                DownloadUrl = "TEST" //_blobClient.GetBlobContainerClient("contracts").GetBlobClient(contract.BlobStoragePath).GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)).ToString()
            };
        }
    }
}
