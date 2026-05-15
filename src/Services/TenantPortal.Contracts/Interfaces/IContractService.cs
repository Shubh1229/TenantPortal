using TenantPortal.Contracts.DTOs;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Contracts.Interfaces
{
    /// <summary>
    /// Manages contract PDF uploads to Azure Blob Storage, metadata persistence,
    /// secure download URL generation, and access control.
    /// Tenants may only access their own contracts.
    /// </summary>
    public interface IContractService
    {
        /// <summary>
        /// Returns all non-deleted contracts visible to the caller.
        /// Tenants receive only their own; Admins and Super Admins receive all.
        /// </summary>
        Task<List<ContractResponseDTO>> GetAllContractsAsync(Guid userId, UserRole role);

        /// <summary>
        /// Returns metadata for a single contract, enforcing tenant ownership for <see cref="UserRole.Tenant"/> callers.
        /// </summary>
        Task<ContractResponseDTO?> GetContractAsync(Guid contractId, Guid userId, UserRole role);

        /// <summary>
        /// Uploads a PDF to Azure Blob Storage, archives any existing current contract
        /// for the same tenant/unit pair, and saves the new contract metadata.
        /// </summary>
        Task<bool> UploadContractAsync(UploadContractRequestDTO request, Guid userId, UserRole role);

        /// <summary>
        /// Generates a short-lived (15-minute) SAS download URL for the contract PDF.
        /// Tenants may only download their own contracts.
        /// </summary>
        Task<ContractDownloadResponseDTO?> DownloadContractAsync(Guid contractId, Guid userId, UserRole role);

        /// <summary>Soft-deletes a contract. Tenants are rejected; Admin and Super Admin only.</summary>
        Task<bool> DeleteContractAsync(Guid contractId, Guid userId, UserRole role);
    }
}
