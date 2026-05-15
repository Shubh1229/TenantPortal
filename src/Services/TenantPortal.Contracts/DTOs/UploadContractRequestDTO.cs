namespace TenantPortal.Contracts.DTOs
{
    public class UploadContractRequestDTO
    {
        public required Guid TenantId { get; set; }
        public required Guid UnitId { get; set; }
        public required IFormFile File { get; set; }
    }
}
