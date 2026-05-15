namespace TenantPortal.Contracts.Models
{
    /// <summary>
    /// Represents an uploaded lease contract PDF. All past and current contracts are retained indefinitely;
    /// superseded contracts are archived by setting <see cref="IsCurrent"/> to <c>false</c>.
    /// Records are soft-deleted; <see cref="IsDeleted"/> is set instead of removing the row.
    /// </summary>
    public class Contract
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>The tenant this contract is associated with.</summary>
        public required Guid TenantId { get; set; }

        /// <summary>The unit this contract covers.</summary>
        public required Guid UnitId { get; set; }

        /// <summary>Path to the PDF blob in Azure Blob Storage (e.g. <c>contracts/{tenantId}/{unitId}/{id}</c>).</summary>
        public required string BlobStoragePath { get; set; }

        /// <summary>Original file name as uploaded by the Admin.</summary>
        public required string FileName { get; set; }

        /// <summary>
        /// <c>true</c> for the active lease; <c>false</c> for archived contracts.
        /// Only one contract per tenant/unit pair should have this set to <c>true</c>.
        /// </summary>
        public bool IsCurrent { get; set; }

        /// <summary>Soft-delete flag.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>UTC timestamp when the contract was soft-deleted. <c>null</c> if not deleted.</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>ID of the Admin or Super Admin who uploaded this contract.</summary>
        public required Guid UploadedBy { get; set; }

        /// <summary>UTC timestamp when the contract was uploaded.</summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>UTC timestamp of the last modification (e.g. when archived).</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
