using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.DTOs
{
    public class UnitPropertyInfoDTO
    {
        public Guid UnitId { get; set; }
        public required string UnitNumber { get; set; }
        public int? Bedrooms { get; set; }
        public decimal? Bathrooms { get; set; }
        public int? SquareFeet { get; set; }
        public BillingMode BillingMode { get; set; }
        public Guid PropertyId { get; set; }
        public required string PropertyName { get; set; }
        public required string PropertyAddress { get; set; }
        public Guid? AdminId { get; set; }
    }
}
