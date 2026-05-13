namespace TenantPortal.Auth.DTOs
{
    public class TotpSetupResponseDTO
    {
        public required string ManualEntryKey { get; set; }
        public required string QrCode { get; set; }
    }
}
