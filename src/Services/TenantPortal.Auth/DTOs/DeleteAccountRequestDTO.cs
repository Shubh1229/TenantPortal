namespace TenantPortal.Auth.DTOs
{
    public class DeleteAccountRequestDTO
    {
        /// <summary>User must type their own email address to confirm deletion.</summary>
        public required string ConfirmEmail { get; set; }
    }
}
