using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.DTOs
{
    public class CreateUnitRequestDTO
    {
        public required Guid PropertyId { get; set; }
        public required string UnitNumber { get; set; }
        public int? Bedrooms { get; set; }
        public decimal? Bathrooms { get; set; }
        public int? SquareFeet { get; set; }
        public BillingMode BillingMode { get; set; } = BillingMode.PerTenant;
    }
}
