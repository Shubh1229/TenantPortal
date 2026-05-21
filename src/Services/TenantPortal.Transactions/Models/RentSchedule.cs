namespace TenantPortal.Transactions.Models
{
    /// <summary>
    /// Defines the recurring rent amount and due day for a tenant.
    /// The nightly overdue job uses this to determine expected payment dates.
    /// </summary>
    public class RentSchedule
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// The tenant this schedule applies to.
        /// <c>null</c> for <see cref="TenantPortal.Shared.Enums.BillingMode.SharedUnit"/> schedules,
        /// which belong to the unit rather than a specific tenant.
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>The unit associated with this rent schedule.</summary>
        public required Guid UnitId { get; set; }

        /// <summary>Monthly rent amount in dollars.</summary>
        public required decimal MonthlyAmount { get; set; }

        /// <summary>
        /// Day of the month rent is due (1–31).
        /// Values above the last day of a given month are clamped by <see cref="TenantPortal.Shared.Helpers.DateHelper"/>.
        /// </summary>
        public required int DueDayOfMonth { get; set; }

        /// <summary>Date the schedule became or becomes effective.</summary>
        public required DateTime StartDate { get; set; }

        /// <summary>Optional date on which this schedule expires. Defaults to one year after StartDate.</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>Whether this schedule has been soft-deleted.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>UTC timestamp when this schedule was soft-deleted, or null if active.</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>ID of the Admin or Super Admin who created this schedule.</summary>
        public required Guid CreatedBy { get; set; }

        /// <summary>UTC timestamp when this record was created.</summary>
        public required DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the last modification.</summary>
        public required DateTime UpdatedAt { get; set; }
    }
}
