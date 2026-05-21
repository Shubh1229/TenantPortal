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
        private readonly BlobContainerClient _blobContainerClient;
        public ContractService(ContractDbContext context, BlobServiceClient blobClient)
        {
            _context = context;
            _blobContainerClient = blobClient.GetBlobContainerClient("contracts");
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
            if (role == UserRole.Tenant || role == UserRole.Tester)
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
            if (role == UserRole.Tenant || role == UserRole.Tester)
            {
                var contracts = await _context.Contracts
                    .Where(c => c.TenantId == userId && !c.IsDeleted)
                    .ToListAsync();
                return contracts.Select(MapToDTO).ToList();
            }

            if (role == UserRole.Admin)
            {
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
            if (role == UserRole.Tenant || role == UserRole.Tester)
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
                await _blobContainerClient.CreateIfNotExistsAsync();
                await _blobContainerClient.GetBlobClient(contract.BlobStoragePath).UploadAsync(request.File.OpenReadStream());
                await _context.Contracts.AddAsync(contract);
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        // Well-known placeholder IDs for demo seed data — same values used by the Transactions seeder.
        private static readonly Guid FakeUnitId     = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        private static readonly Guid FakeUploaderId = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");

        public async Task<bool> SeedTesterDataAsync(Guid tenantId)
        {
            // Idempotent — skip if a contract for this tester already exists
            if (await _context.Contracts.AnyAsync(c => c.TenantId == tenantId && !c.IsDeleted))
                return true;

            try
            {
                var contractId = Guid.NewGuid();
                var blobPath = $"contracts/{tenantId}/{FakeUnitId}/{contractId}";

                // Read the embedded demo PDF
                var assembly = typeof(ContractService).Assembly;
                var resourceName = assembly.GetManifestResourceNames()
                    .First(n => n.EndsWith("fake_lease_contract.pdf", StringComparison.OrdinalIgnoreCase));
                using var pdfStream = assembly.GetManifestResourceStream(resourceName)!;

                // Upload to blob storage
                await _blobContainerClient.CreateIfNotExistsAsync();
                await _blobContainerClient.GetBlobClient(blobPath).UploadAsync(pdfStream, overwrite: true);

                await _context.Contracts.AddAsync(new Contract
                {
                    Id = contractId,
                    TenantId = tenantId,
                    UnitId = FakeUnitId,
                    FileName = "742-Willow-Creek-Unit3B-Lease-2026.pdf",
                    BlobStoragePath = blobPath,
                    IsCurrent = true,
                    IsDeleted = false,
                    UploadedBy = FakeUploaderId,
                    UploadedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private ContractResponseDTO MapToDTO(Contract contract)
        {
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
            return new ContractResponseDTO
            {
                Id = contract.Id,
                TenantId = contract.TenantId,
                FileName = contract.FileName,
                IsCurrent = contract.IsCurrent,
                UploadedAt = contract.UploadedAt,
                DownloadUrl = GenerateSasUrl(contract.BlobStoragePath, expiresOn),
                PreviewUrl = GenerateSasUrl(contract.BlobStoragePath, expiresOn, inline: true),
            };
        }

        /// <summary>
        /// Generates a time-limited Azure Blob SAS read URL.
        /// Pass <paramref name="inline"/> = true to add a content-disposition header so browsers
        /// render the PDF in-page rather than prompting a download.
        /// Falls back to an empty string when the BlobServiceClient lacks shared-key credentials.
        /// </summary>
        private string GenerateSasUrl(string blobPath, DateTimeOffset expiresOn, bool inline = false)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(blobPath);
                if (!inline)
                    return blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, expiresOn).ToString();

                var builder = new Azure.Storage.Sas.BlobSasBuilder(Azure.Storage.Sas.BlobSasPermissions.Read, expiresOn)
                {
                    BlobContainerName = _blobContainerClient.Name,
                    BlobName = blobPath,
                    Resource = "b",
                    ContentDisposition = "inline",
                };
                return blobClient.GenerateSasUri(builder).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
