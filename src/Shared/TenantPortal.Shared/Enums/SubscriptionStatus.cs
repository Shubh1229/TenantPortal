namespace TenantPortal.Shared.Enums
{
    /// <summary>
    /// Lifecycle state of an Admin's SaaS subscription.
    /// Maps directly to Stripe subscription statuses.
    /// </summary>
    public enum SubscriptionStatus
    {
        /// <summary>No subscription has been created yet (account just registered, checkout not completed).</summary>
        None = 0,

        /// <summary>Free trial period is active. Full access is granted.</summary>
        Trialing = 1,

        /// <summary>Subscription is current and the most recent invoice was paid successfully.</summary>
        Active = 2,

        /// <summary>
        /// The most recent invoice payment failed. Stripe is retrying.
        /// The account is suspended until payment succeeds or the subscription is canceled.
        /// </summary>
        PastDue = 3,

        /// <summary>
        /// The subscription has been permanently canceled, either by the admin
        /// or automatically after all retry attempts failed.
        /// </summary>
        Canceled = 4
    }
}
