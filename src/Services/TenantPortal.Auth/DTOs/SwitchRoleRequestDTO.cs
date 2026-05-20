namespace TenantPortal.Auth.DTOs
{
    public class SwitchRoleRequestDTO
    {
        /// <summary>Target role string, e.g. "Admin", "Tenant", "Tester", "SuperAdmin".</summary>
        public required string TargetRole { get; set; }
    }
}
