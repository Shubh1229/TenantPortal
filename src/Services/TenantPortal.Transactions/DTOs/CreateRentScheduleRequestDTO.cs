namespace TenantPortal.Transactions.DTOs
{
    public class CreateRentScheduleRequestDTO
    {
        public required Guid TenantId { get; set; }
        public required Guid UnitId { get; set; }
        public required decimal MonthlyAmount { get; set; }
        public required int DueDayOfMonth { get; set; }
        public required DateTime StartDate { get; set; }
    }
}
