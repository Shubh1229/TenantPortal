namespace TenantPortal.Shared.Enums
{
    public enum BillingMode
    {
        /// <summary>Each tenant on the unit has their own rent schedule and payment history.</summary>
        PerTenant = 0,

        /// <summary>One shared rent schedule for the unit; any assigned tenant can pay toward it.</summary>
        SharedUnit = 1,
    }
}
