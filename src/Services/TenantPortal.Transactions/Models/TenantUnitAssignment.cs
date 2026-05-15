namespace TenantPortal.Transactions.Models
{
    /// <summary>
    /// Links a tenant to a specific unit for a date range, maintaining full tenancy history.
    /// A <c>null</c> <see cref="EndDate"/> indicates the assignment is currently active.
    /// </summary>
    public class TenantUnitAssignment
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>The tenant occupying the unit.</summary>
        public required Guid TenantId { get; set; }

        /// <summary>The unit being occupied.</summary>
        public required Guid UnitId { get; set; }

        /// <summary>Date the tenant moved in (start of tenancy).</summary>
        public required DateTime StartDate { get; set; }

        /// <summary>Date the tenant moved out. <c>null</c> while the tenancy is active.</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>UTC timestamp when the record was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the last modification.</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
