namespace TenantPortal.Transactions.DTOs
{
    public class UpdateRentScheduleRequestDTO
    {
        public required Guid RentScheduleId { get; set; }
        public decimal? MonthlyAmount { get; set; }
        public int? DueDayOfMonth { get; set; }
    }
}