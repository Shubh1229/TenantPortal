using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.DTOs
{
    /// <summary>
    /// Request body for a tenant submitting an external payment claim
    /// (Zelle, cheque, bank transfer, etc.) that requires admin approval.
    /// </summary>
    public class ExternalPaymentRequestDTO
    {
        /// <summary>The unit the payment is for.</summary>
        public required Guid UnitId { get; set; }

        /// <summary>Amount paid by the tenant.</summary>
        public required decimal Amount { get; set; }

        /// <summary>The method used to make the external payment.</summary>
        public required PaymentMethod PaymentMethod { get; set; }

        /// <summary>Date on which the tenant made the external payment.</summary>
        public required DateTime PaidDate { get; set; }

        /// <summary>Optional note from the tenant (e.g. reference number, confirmation screenshot link).</summary>
        public string? Note { get; set; }
    }
}
