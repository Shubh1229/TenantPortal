using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Models;

namespace TenantPortal.Transactions.Interfaces
{
    /// <summary>
    /// Manages rent schedules that define a tenant's monthly payment amount and due day.
    /// Admin and Super Admin only — tenants may read but not create or modify.
    /// </summary>
    public interface IRentScheduleService
    {
        /// <summary>Returns the rent schedule for the specified tenant, or <c>null</c> if none exists.</summary>
        Task<RentSchedule?> GetRentScheduleAsync(Guid tenantId);

        /// <summary>
        /// Returns the schedule most relevant to a tenant — checks per-tenant first,
        /// then falls back to the unit's shared schedule if one exists.
        /// </summary>
        Task<RentSchedule?> GetMyRentScheduleAsync(Guid tenantId);

        /// <summary>Returns all rent schedules. Admins see only schedules they created; SuperAdmin sees all.</summary>
        Task<List<RentSchedule>> GetAllRentSchedulesAsync(Guid userId, UserRole role);

        /// <summary>Creates a new rent schedule. Rejects callers with the <see cref="UserRole.Tenant"/> role.</summary>
        Task<bool> CreateRentScheduleAsync(CreateRentScheduleRequestDTO request, Guid userId, UserRole role);

        /// <summary>Updates an existing rent schedule's amount or due day. Rejects <see cref="UserRole.Tenant"/> callers.</summary>
        Task<bool> UpdateRentScheduleAsync(UpdateRentScheduleRequestDTO request, Guid userId, UserRole role);

        /// <summary>
        /// Calculates the next due date following <paramref name="refDate"/>,
        /// clamping to the last valid day of February and 30-day months.
        /// </summary>
        /// <param name="schedule">The schedule providing the configured due day of month.</param>
        /// <param name="refDate">The reference date from which to advance one month.</param>
        DateTime CalculateNextDueDate(RentSchedule schedule, DateTime refDate);
    }
}
