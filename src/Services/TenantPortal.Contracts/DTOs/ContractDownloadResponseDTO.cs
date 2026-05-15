namespace TenantPortal.Contracts.DTOs
{
    public class ContractDownloadResponseDTO
    {
        public required string DownloadUrl { get; set; }
        public required string FileName { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
