namespace TenantPortal.Transactions.Models
{
    /// <summary>
    /// A rentable unit within a <see cref="Property"/>.
    /// Transactions and rent schedules are always associated with a unit.
    /// Records are soft-deleted; <see cref="IsDeleted"/> is set instead of removing the row.
    /// </summary>
    public class Unit
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>The property this unit belongs to.</summary>
        public required Guid PropertyId { get; set; }

        /// <summary>Human-readable unit identifier (e.g. "101", "2B").</summary>
        public required string UnitNumber { get; set; }

        /// <summary>Number of bedrooms. <c>null</c> if not specified.</summary>
        public int? Bedrooms { get; set; }

        /// <summary>Number of bathrooms (supports half-baths, e.g. 1.5). <c>null</c> if not specified.</summary>
        public decimal? Bathrooms { get; set; }

        /// <summary>Floor area in square feet. <c>null</c> if not specified.</summary>
        public int? SquareFeet { get; set; }

        /// <summary>Whether the unit is currently available or occupied.</summary>
        public required bool IsActive { get; set; }

        /// <summary>Soft-delete flag.</summary>
        public required bool IsDeleted { get; set; }

        /// <summary>UTC timestamp when the unit was soft-deleted. <c>null</c> if not deleted.</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>UTC timestamp when the record was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the last modification.</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
