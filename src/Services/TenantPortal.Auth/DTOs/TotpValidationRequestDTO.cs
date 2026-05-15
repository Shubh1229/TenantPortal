namespace TenantPortal.Auth.DTOs
{
    public class TotpValidationRequestDTO
    {
        public required string TemporaryToken { get; set; }
        public required string TotpCode { get; set; }
    }
}
