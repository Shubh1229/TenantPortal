namespace TenantPortal.Transactions.Models
{
    public class Unit
    {
        public required Guid Id {  get; set; }
        public required Guid PropertyId {  get; set; }
        public required string UnitNumber { get; set; }
        public int? Bedrooms { get; set; }
        public decimal? Bathrooms { get; set; }
        public int? SquareFeet {  get; set; }
        public required bool IsActive { get; set; }
        public required bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt {  get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
