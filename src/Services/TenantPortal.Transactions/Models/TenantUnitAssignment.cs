namespace TenantPortal.Transactions.Models
{
    public class TenantUnitAssignment
    {
        public required Guid Id { get; set; }
        public required Guid TenantId { get; set; }
        public required Guid UnitId { get; set; }
        public required DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
