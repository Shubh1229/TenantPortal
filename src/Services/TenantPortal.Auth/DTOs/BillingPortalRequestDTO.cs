namespace TenantPortal.Auth.DTOs
{
    /// <summary>Payload for the Stripe Billing Portal redirect endpoint.</summary>
    public class BillingPortalRequestDTO
    {
        /// <summary>URL the Stripe portal redirects back to after the admin closes it.</summary>
        public required string ReturnUrl { get; set; }
    }
}
