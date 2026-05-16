namespace TenantPortal.Auth.DTOs
{
    /// <summary>
    /// Response returned after a successful Admin self-registration.
    /// The caller must redirect to <see cref="CheckoutUrl"/> to activate the account
    /// and simultaneously display the TOTP setup so the admin can enrol their authenticator.
    /// </summary>
    public class AdminRegisterResponseDTO
    {
        /// <summary>
        /// Stripe Checkout Session URL. The admin must complete this to activate their account.
        /// The URL is single-use and expires after 24 hours.
        /// </summary>
        public required string CheckoutUrl { get; set; }

        /// <summary>
        /// TOTP enrolment data. The admin should scan the QR code (or enter the manual key)
        /// into their authenticator app before completing checkout, so they are ready to log in
        /// once Stripe confirms the subscription.
        /// </summary>
        public required TotpSetupResponseDTO TotpSetup { get; set; }
    }
}
