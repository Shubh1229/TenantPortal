namespace TenantPortal.Contracts.DTOs
{
    /// <summary>Response returned by the download endpoint containing a time-limited SAS URL.</summary>
    public class ContractDownloadResponseDTO
    {
        /// <summary>Pre-signed Azure Blob Storage SAS URL valid for 15 minutes.</summary>
        public required string DownloadUrl { get; set; }

        /// <summary>Original file name of the PDF (for use as the browser download filename).</summary>
        public required string FileName { get; set; }

        /// <summary>UTC time after which the <see cref="DownloadUrl"/> is no longer valid.</summary>
        public DateTime ExpiresAt { get; set; }
    }
}
