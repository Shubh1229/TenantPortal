namespace TenantPortal.Contracts.DTOs
{
    /// <summary>Form-data request body for uploading a contract PDF.</summary>
    public class UploadContractRequestDTO
    {
        /// <summary>The tenant the contract is associated with.</summary>
        public required Guid TenantId { get; set; }

        /// <summary>The unit this lease covers.</summary>
        public required Guid UnitId { get; set; }

        /// <summary>The PDF file to upload. Must be <c>application/pdf</c> and within the maximum allowed file size.</summary>
        public required IFormFile File { get; set; }
    }
}
