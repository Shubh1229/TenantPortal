namespace TenantPortal.Notifications.DTOs
{
    public class TesterActionDTO
    {
        public required string TesterEmail { get; set; }
        public required string Action { get; set; }
        public string? Body { get; set; }
    }
}
