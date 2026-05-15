namespace TenantPortal.Transactions.Models
{
    public class Property
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required bool IsActive { get; set; }
        public required bool IsDeleted { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
