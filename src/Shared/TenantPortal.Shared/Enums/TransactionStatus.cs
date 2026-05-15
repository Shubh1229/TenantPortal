namespace TenantPortal.Shared.Enums
{
    /// <summary>
    /// Lifecycle states of a transaction record.
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>External payment submitted by a tenant and awaiting admin approval.</summary>
        Pending,

        /// <summary>Payment completed — either via a successful Stripe webhook or admin approval of an external request.</summary>
        Confirmed,

        /// <summary>Admin rejected an external payment request. Overdue cycle resumes from the declined date.</summary>
        Declined,

        /// <summary>Past the due date with no Confirmed or Pending payment. Set by the nightly <c>OverduePaymentJob</c>.</summary>
        Overdue
    }
}
