using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.DTOs
{
    public class ExternalPaymentRequestDTO
    {
        public required Guid UnitId { get; set; }
        public required decimal Amount { get; set; }
        public required PaymentMethod PaymentMethod { get; set; }
        public required DateTime PaidDate { get; set; }
        public string? Note { get; set; }
    }
}