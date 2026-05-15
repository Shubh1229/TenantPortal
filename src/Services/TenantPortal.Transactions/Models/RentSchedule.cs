namespace TenantPortal.Transactions.Models
{
    public class RentSchedule
    {
        public required Guid Id { get; set; }
        public required Guid TenantId { get; set; }
        public required Guid UnitId { get; set; }
        public required decimal MonthlyAmount { get; set; }
        public required int DueDayOfMonth { get; set; }
        public required DateTime StartDate { get; set; }
        public required Guid CreatedBy { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
