namespace TenantPortal.Notifications.DTOs
{
    public class TrialEndingRequestDTO
    {
        public Guid UserId { get; set; }
        public required string Email { get; set; }
        public int DaysLeft { get; set; }
    }
}
