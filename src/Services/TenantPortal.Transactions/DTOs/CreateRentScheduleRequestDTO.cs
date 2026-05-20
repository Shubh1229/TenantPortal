namespace TenantPortal.Transactions.DTOs
{
    /// <summary>Request body for creating a new rent schedule.</summary>
    public class CreateRentScheduleRequestDTO
    {
        /// <summary>
        /// The tenant the schedule applies to. Required for PerTenant units.
        /// Omit (or set null) for SharedUnit schedules — the schedule belongs to the unit, not a specific tenant.
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>The unit associated with this rent schedule.</summary>
        public required Guid UnitId { get; set; }

        /// <summary>Monthly rent amount in dollars.</summary>
        public required decimal MonthlyAmount { get; set; }

        /// <summary>Day of the month rent is due (1–31). Values beyond the last day of any month are clamped automatically.</summary>
        public required int DueDayOfMonth { get; set; }

        /// <summary>Date on which this schedule takes effect.</summary>
        public required DateTime StartDate { get; set; }
    }
}
