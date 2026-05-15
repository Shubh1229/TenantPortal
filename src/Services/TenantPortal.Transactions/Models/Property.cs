namespace TenantPortal.Transactions.Models
{
    /// <summary>
    /// Represents a rental property (building or complex).
    /// Properties contain one or more <see cref="Unit"/> records.
    /// Records are soft-deleted; <see cref="IsDeleted"/> is set instead of removing the row.
    /// </summary>
    public class Property
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>Display name for the property (e.g. "Maple Street Apartments").</summary>
        public required string Name { get; set; }

        /// <summary>Full mailing address of the property.</summary>
        public required string Address { get; set; }

        /// <summary>Whether the property is currently in use.</summary>
        public required bool IsActive { get; set; }

        /// <summary>Soft-delete flag.</summary>
        public required bool IsDeleted { get; set; }

        /// <summary>UTC timestamp when the record was created.</summary>
        public required DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the last modification.</summary>
        public required DateTime UpdatedAt { get; set; }

        /// <summary>UTC timestamp when the property was soft-deleted. <c>null</c> if not deleted.</summary>
        public DateTime? DeletedAt { get; set; }
    }
}
