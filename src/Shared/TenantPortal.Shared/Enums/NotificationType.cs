namespace TenantPortal.Shared.Enums
{
    /// <summary>
    /// Distinguishes the event that triggered a notification.
    /// Used for both in-app notifications and email subject/body templating.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>A Stripe payment succeeded or an external payment was approved by an Admin.</summary>
        PaymentConfirmed,

        /// <summary>A tenant submitted an external payment request awaiting admin review.</summary>
        PaymentPending,

        /// <summary>An Admin declined a tenant's external payment request.</summary>
        PaymentDeclined,

        /// <summary>A transaction is past its due date with no confirmed or pending payment.</summary>
        PaymentOverdue,

        /// <summary>A configured rent reminder fired N days before the due date.</summary>
        RentReminder,

        /// <summary>An account invitation was sent to a new Admin or Tenant.</summary>
        InviteCreated,

        /// <summary>An Admin uploaded a new lease contract for a tenant.</summary>
        ContractUploaded
    }
}
