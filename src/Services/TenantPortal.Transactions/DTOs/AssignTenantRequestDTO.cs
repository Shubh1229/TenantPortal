namespace TenantPortal.Transactions.DTOs
{
    public class AssignTenantRequestDTO
    {
        public required Guid TenantId { get; set; }
        public required DateTime StartDate { get; set; }
    }
}
