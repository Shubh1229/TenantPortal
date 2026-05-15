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
            RentSchedule schedule = new RentSchedule
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UnitId = request.UnitId,
                MonthlyAmount = request.MonthlyAmount,
                DueDayOfMonth = request.DueDayOfMonth,
                StartDate = request.StartDate,
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
            var schedule = await _context.RentSchedules.FirstOrDefaultAsync(s => s.TenantId == tenantId);
            return schedule;
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
                schedule.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
