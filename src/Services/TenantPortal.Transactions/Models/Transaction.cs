using TenantPortal.Shared.Enums;

namespace TenantPortal.Transactions.Models
{
    /// <summary>
    /// Records a financial event between a tenant and the property.
    /// Covers rent, fees, deposits, refunds, and any other charge type.
    /// Records are soft-deleted; <see cref="IsDeleted"/> is set instead of removing the row.
    /// </summary>
    public class Transaction
    {
        /// <summary>Primary key.</summary>
        public required Guid Id { get; set; }

        /// <summary>The tenant this transaction belongs to.</summary>
        public required Guid TenantId { get; set; }

        /// <summary>The unit associated with this charge.</summary>
        public required Guid UnitId { get; set; }

        /// <summary>Category of the charge or credit.</summary>
        public required TransactionType Type { get; set; }

        /// <summary>Dollar amount of the transaction. Positive values are charges; negative values are credits/refunds.</summary>
        public required decimal Amount { get; set; }

        /// <summary>Current lifecycle state of this transaction.</summary>
        public required TransactionStatus Status { get; set; }

        /// <summary>How the payment was or will be made.</summary>
        public required PaymentMethod PaymentMethod { get; set; }

        /// <summary>Optional free-text note from the tenant describing the external payment method (e.g. "Zelle sent 2pm").</summary>
        public string? ExternalMethodNote { get; set; }

        /// <summary>Stripe PaymentIntent ID for Stripe-originated payments. <c>null</c> for external or manual transactions.</summary>
        public string? StripePaymentIntentId { get; set; }

        /// <summary>The date payment was originally due. <c>null</c> for manual backfill entries without a scheduled due date.</summary>
        public DateTime? DueDate { get; set; }

        /// <summary>The date payment was actually received or submitted. <c>null</c> until confirmed.</summary>
        public DateTime? PaidDate { get; set; }

        /// <summary>Soft-delete flag. Excluded from all queries when <c>true</c>.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>UTC timestamp when the record was soft-deleted. <c>null</c> if not deleted.</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>ID of the user who created this record (Admin for manual entries; TenantId for external submissions).</summary>
        public required Guid CreatedBy { get; set; }

        /// <summary>UTC timestamp when this record was created.</summary>
        public required DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the last status change or edit.</summary>
        public required DateTime UpdatedAt { get; set; }
    }
}
