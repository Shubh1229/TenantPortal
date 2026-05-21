namespace TenantPortal.Transactions.DTOs
{
    /// <summary>Request body for updating an existing rent schedule. All fields are optional — only provided fields are applied.</summary>
    public class UpdateRentScheduleRequestDTO
    {
        /// <summary>The rent schedule to update. Populated from the route parameter by the controller.</summary>
        public required Guid RentScheduleId { get; set; }

        /// <summary>New monthly rent amount, if being updated.</summary>
        public decimal? MonthlyAmount { get; set; }

        /// <summary>New due day of month (1–31), if being updated.</summary>
        public int? DueDayOfMonth { get; set; }

        /// <summary>New end date, if being updated.</summary>
        public DateTime? EndDate { get; set; }
    }
}
