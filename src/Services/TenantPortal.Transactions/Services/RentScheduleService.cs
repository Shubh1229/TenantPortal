using Microsoft.EntityFrameworkCore;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Helpers;
using TenantPortal.Transactions.Data;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Interfaces;
using TenantPortal.Transactions.Models;

namespace TenantPortal.Transactions.Services
{
    public class RentScheduleService : IRentScheduleService
    {
        private readonly TransactionDbContext _context;
        public RentScheduleService(TransactionDbContext context)
        {
            _context = context;
        }

        public DateTime CalculateNextDueDate(RentSchedule schedule, DateTime refDate)
        {
            var targetYear = refDate.Year;
            var targetMonth = refDate.Month;
            if (targetMonth == 12)
            {
                targetMonth = 1;
                targetYear++;
            } else
            {
                targetMonth++;
            }
            return DateHelper.GetAdjustedDueDate(schedule.DueDayOfMonth, targetYear, targetMonth);
        }

        public async Task<bool> CreateRentScheduleAsync(CreateRentScheduleRequestDTO request, Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant) return false;

            // Validate: PerTenant schedules require a TenantId; SharedUnit schedules must not have one.
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == request.UnitId && !u.IsDeleted);
            if (unit == null) return false;

            if (unit.BillingMode == BillingMode.PerTenant && request.TenantId == null) return false;
            if (unit.BillingMode == BillingMode.SharedUnit) request.TenantId = null;

            var startUtc = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
            RentSchedule schedule = new RentSchedule
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UnitId = request.UnitId,
                MonthlyAmount = request.MonthlyAmount,
                DueDayOfMonth = request.DueDayOfMonth,
                StartDate = startUtc,
                EndDate = request.EndDate.HasValue
                    ? DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc)
                    : startUtc.AddYears(1),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            try
            {
                await _context.RentSchedules.AddAsync(schedule);
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception) { return false; }
        }

        public async Task<RentSchedule?> GetRentScheduleAsync(Guid tenantId)
        {
            return await _context.RentSchedules.FirstOrDefaultAsync(s => s.TenantId == tenantId);
        }

        /// <summary>
        /// Returns the rent schedule relevant to a tenant — handles both PerTenant and SharedUnit modes.
        /// Finds the tenant's active unit assignment, then returns the appropriate schedule.
        /// </summary>
        public async Task<RentSchedule?> GetMyRentScheduleAsync(Guid tenantId)
        {
            // First check for a per-tenant schedule (works regardless of billing mode)
            var perTenantSchedule = await _context.RentSchedules
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);
            if (perTenantSchedule != null) return perTenantSchedule;

            // Fall back to shared-unit schedule via the tenant's active assignment
            var assignment = await _context.TenantUnitAssignments
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.EndDate == null);
            if (assignment == null) return null;

            return await _context.RentSchedules
                .FirstOrDefaultAsync(s => s.UnitId == assignment.UnitId && s.TenantId == null);
        }

        public async Task<List<RentSchedule>> GetAllRentSchedulesAsync(Guid userId, UserRole role)
        {
            var query = _context.RentSchedules.AsQueryable();
            if (role == UserRole.Admin)
                query = query.Where(r => r.CreatedBy == userId);
            return await query.ToListAsync();
        }

        public async Task<bool> UpdateRentScheduleAsync(UpdateRentScheduleRequestDTO request, Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant) return false;
            try
            {
                var schedule = await _context.RentSchedules.FirstOrDefaultAsync(s => s.Id == request.RentScheduleId);
                if (schedule == null) return false;
                if (request.DueDayOfMonth != null) schedule.DueDayOfMonth = request.DueDayOfMonth.Value;
                if (request.MonthlyAmount != null) schedule.MonthlyAmount = request.MonthlyAmount.Value;
                if (request.EndDate != null) schedule.EndDate = DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
                schedule.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception) { return false; }
        }

        public async Task<bool> DeleteRentScheduleAsync(Guid id, Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant) return false;
            try
            {
                var schedule = await _context.RentSchedules.FirstOrDefaultAsync(s => s.Id == id);
                if (schedule == null) return false;
                _context.RentSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
