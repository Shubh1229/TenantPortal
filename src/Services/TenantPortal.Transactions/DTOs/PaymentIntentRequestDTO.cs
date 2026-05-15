namespace TenantPortal.Transactions.DTOs
{
    public class PaymentIntentRequestDTO
    {
        public required decimal Amount { get; set; }
        public required string Currency { get; set; } = "usd";
        public required Guid RentScheduleId { get; set; }
    }
}
