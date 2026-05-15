namespace TenantPortal.Contracts.DTOs
{
    public class ContractResponseDTO
    {
        public required Guid Id { get; set; }
        public required Guid TenantId { get; set; }
        public required string FileName { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime UploadedAt { get; set; }
        public required string DownloadUrl { get; set; }
    }
}
