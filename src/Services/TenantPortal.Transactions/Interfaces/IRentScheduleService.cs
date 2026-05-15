using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Models;

namespace TenantPortal.Transactions.Interfaces
{
    public interface IRentScheduleService
    {
        Task<RentSchedule?> GetRentScheduleAsync(Guid tenantId);
        Task<bool> CreateRentScheduleAsync(CreateRentScheduleRequestDTO request, Guid userId, UserRole role);
        Task<bool> UpdateRentScheduleAsync(UpdateRentScheduleRequestDTO request, Guid userId, UserRole role);
        DateTime CalculateNextDueDate(RentSchedule schedule, DateTime refDate);
    }
}
