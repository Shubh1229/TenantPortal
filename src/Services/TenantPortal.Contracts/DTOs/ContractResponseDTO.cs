namespace TenantPortal.Contracts.DTOs
{
    /// <summary>Contract metadata returned by list and detail endpoints.</summary>
    public class ContractResponseDTO
    {
        /// <summary>Contract record ID.</summary>
        public required Guid Id { get; set; }

        /// <summary>The tenant this contract belongs to.</summary>
        public required Guid TenantId { get; set; }

        /// <summary>Original file name of the uploaded PDF.</summary>
        public required string FileName { get; set; }

        /// <summary><c>true</c> if this is the active lease; <c>false</c> if archived.</summary>
        public bool IsCurrent { get; set; }

        /// <summary>UTC timestamp when the contract was uploaded.</summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>Pre-signed URL for in-browser viewing or downloading the PDF.</summary>
        public required string DownloadUrl { get; set; }
    }
}
