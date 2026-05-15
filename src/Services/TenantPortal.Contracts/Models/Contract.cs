namespace TenantPortal.Contracts.Models
{
    public class Contract
    {
        public required Guid Id { get; set; }
        public required Guid TenantId { get; set; }
        public required Guid UnitId { get; set; }
        public required string BlobStoragePath { get; set; }
        public required string FileName { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public required Guid UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
