using TenantPortal.Contracts.DTOs;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Contracts.Interfaces
{
    public interface IContractService
    {
        Task<List<ContractResponseDTO>> GetAllContractsAsync(Guid userId, UserRole role);
        Task<ContractResponseDTO?> GetContractAsync(Guid contractId, Guid userId, UserRole role);
        Task<bool> UploadContractAsync(UploadContractRequestDTO request, Guid userId, UserRole role);
        Task<ContractDownloadResponseDTO?> DownloadContractAsync(Guid contractId, Guid userId, UserRole role);
        Task<bool> DeleteContractAsync(Guid contractId, Guid userId, UserRole role);
    }
}
