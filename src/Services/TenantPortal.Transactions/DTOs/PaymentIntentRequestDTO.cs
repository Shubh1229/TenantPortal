namespace TenantPortal.Transactions.DTOs
{
    /// <summary>Request body for creating a Stripe PaymentIntent for an in-app card payment.</summary>
    public class PaymentIntentRequestDTO
    {
        /// <summary>Amount to charge in the specified currency.</summary>
        public required decimal Amount { get; set; }

        /// <summary>ISO 4217 currency code (default: <c>usd</c>).</summary>
        public required string Currency { get; set; } = "usd";

        /// <summary>The rent schedule this payment is for. Stored in the PaymentIntent metadata for webhook correlation.</summary>
        public required Guid RentScheduleId { get; set; }
    }
}
