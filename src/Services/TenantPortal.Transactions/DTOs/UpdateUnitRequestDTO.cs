using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.DTOs
{
    public class UpdateUnitRequestDTO
    {
        public string? UnitNumber { get; set; }
        public int? Bedrooms { get; set; }
        public decimal? Bathrooms { get; set; }
        public int? SquareFeet { get; set; }
        public BillingMode? BillingMode { get; set; }
    }
}
