using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.Data;
using TenantPortal.Transactions.DTOs;
using TenantPortal.Transactions.Interfaces;
using TenantPortal.Transactions.Models;
using Microsoft.EntityFrameworkCore;

namespace TenantPortal.Transactions.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly TransactionDbContext _context;
        public TransactionService(TransactionDbContext context) 
        {
            _context = context;
        }
        public async Task<bool> ApproveExternalPaymentRequestAsync(Guid transactionId)
        {
            try
            {
                var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.Status == TransactionStatus.Pending);
                if (transaction == null)
                {
                    return false;
                }
                transaction.Status = TransactionStatus.Confirmed;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CreateTransactionAsync(CreateTransactionRequestDTO request, Guid createdBy)
        {
            try
            {
                Transaction transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    Amount = request.Amount,
                    UnitId = request.UnitId,
                    Type = request.Type,
                    Status = TransactionStatus.Confirmed,
                    PaymentMethod = request.PaymentMethod,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _context.Transactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeclineExternalPaymentRequestAsync(Guid transactionId)
        {
            try
            {
                var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.Status == TransactionStatus.Pending);
                if (transaction == null)
                {
                    return false;
                }
                transaction.Status = TransactionStatus.Declined;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Transaction>> GetAllTransactionsAsync(Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant || role == UserRole.Tester)
            {
                return await _context.Transactions
                    .Where(t => t.TenantId == userId && !t.IsDeleted)
                    .ToListAsync();
            }

            if (role == UserRole.Admin)
            {
                // Scope to units inside properties this admin owns.
                // EF Core translates the two subqueries into a single SQL with EXISTS/IN clauses.
                var adminPropertyIds = _context.Properties
                    .Where(p => p.AdminId == userId && !p.IsDeleted)
                    .Select(p => p.Id);

                var adminUnitIds = _context.Units
                    .Where(u => adminPropertyIds.Contains(u.PropertyId) && !u.IsDeleted)
                    .Select(u => u.Id);

                return await _context.Transactions
                    .Where(t => adminUnitIds.Contains(t.UnitId) && !t.IsDeleted)
                    .ToListAsync();
            }

            // SuperAdmin sees everything
            return await _context.Transactions.Where(t => !t.IsDeleted).ToListAsync();
        }

        public async Task<Transaction?> GetTransactionAsync(Guid transactionId, Guid userId, UserRole role)
        {
            if (role == UserRole.Tenant || role == UserRole.Tester)
            {
                return await _context.Transactions.FirstOrDefaultAsync(t =>
                    t.Id == transactionId && t.TenantId == userId && !t.IsDeleted);
            }

            if (role == UserRole.Admin)
            {
                // Verify the transaction belongs to a unit in one of this admin's properties
                var adminPropertyIds = _context.Properties
                    .Where(p => p.AdminId == userId && !p.IsDeleted)
                    .Select(p => p.Id);

                var adminUnitIds = _context.Units
                    .Where(u => adminPropertyIds.Contains(u.PropertyId) && !u.IsDeleted)
                    .Select(u => u.Id);

                return await _context.Transactions.FirstOrDefaultAsync(t =>
                    t.Id == transactionId && adminUnitIds.Contains(t.UnitId) && !t.IsDeleted);
            }

            // SuperAdmin can access any transaction
            return await _context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted);
        }

        public async Task<bool> SoftDeleteTransactionAsync(Guid transactionId, Guid userId)
        {
            try
            {    
                var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.CreatedBy == userId && !t.IsDeleted);
                if (transaction == null) { return false; }
                transaction.IsDeleted = true;
                transaction.DeletedAt = DateTime.UtcNow;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SubmitExternalPaymentRequestAsync(ExternalPaymentRequestDTO request, Guid tenantId)
        {
            try
            {
                Transaction transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Amount = request.Amount,
                    UnitId = request.UnitId,
                    Type = TransactionType.Rent,
                    Status = TransactionStatus.Pending,
                    PaymentMethod = request.PaymentMethod,
                    ExternalMethodNote = request.Note,
                    PaidDate = DateTime.SpecifyKind(request.PaidDate, DateTimeKind.Utc),
                    CreatedBy = tenantId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _context.Transactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                return true;
            } catch (Exception)
            {
                return false;
            }
        }

        // Well-known placeholder IDs for demo seed data — same values used by the Contracts seeder.
        private static readonly Guid FakeUnitId   = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        private static readonly Guid FakeAdminId  = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");

        public async Task<bool> SeedTesterDataAsync(Guid tenantId)
        {
            // Idempotent — skip if seed data already exists for this tester
            if (await _context.Transactions.AnyAsync(t => t.TenantId == tenantId && !t.IsDeleted))
                return true;

            try
            {
                // Rent schedule: $1,850/month due on the 1st, parking rolled into transactions
                await _context.RentSchedules.AddAsync(new RentSchedule
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UnitId = FakeUnitId,
                    MonthlyAmount = 1850m,
                    DueDayOfMonth = 1,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = FakeAdminId,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });

                // 6 confirmed payments Jan–Jun 2026 ($1,925 = $1,850 rent + $75 parking)
                var payments = new[]
                {
                    (due: new DateTime(2026, 1,  1, 0,0,0, DateTimeKind.Utc), paid: new DateTime(2026, 1,  2, 0,0,0, DateTimeKind.Utc), method: PaymentMethod.Stripe),
                    (due: new DateTime(2026, 2,  1, 0,0,0, DateTimeKind.Utc), paid: new DateTime(2026, 2,  1, 0,0,0, DateTimeKind.Utc), method: PaymentMethod.Stripe),
                    (due: new DateTime(2026, 3,  1, 0,0,0, DateTimeKind.Utc), paid: new DateTime(2026, 3,  2, 0,0,0, DateTimeKind.Utc), method: PaymentMethod.Stripe),
                    (due: new DateTime(2026, 4,  1, 0,0,0, DateTimeKind.Utc), paid: new DateTime(2026, 4,  5, 0,0,0, DateTimeKind.Utc), method: PaymentMethod.Stripe),
                    (due: new DateTime(2026, 5,  1, 0,0,0, DateTimeKind.Utc), paid: new DateTime(2026, 5,  2, 0,0,0, DateTimeKind.Utc), method: PaymentMethod.Stripe),
                    (due: new DateTime(2026, 6,  1, 0,0,0, DateTimeKind.Utc), paid: new DateTime(2026, 6,  2, 0,0,0, DateTimeKind.Utc), method: PaymentMethod.External),
                };

                foreach (var p in payments)
                {
                    await _context.Transactions.AddAsync(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        UnitId = FakeUnitId,
                        Type = TransactionType.Rent,
                        Amount = 1925m,
                        Status = TransactionStatus.Confirmed,
                        PaymentMethod = p.method,
                        DueDate = p.due,
                        PaidDate = p.paid,
                        IsDeleted = false,
                        CreatedBy = FakeAdminId,
                        CreatedAt = p.paid,
                        UpdatedAt = p.paid,
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<UnitPropertyInfoDTO?> GetMyUnitInfoAsync(Guid tenantId)
        {
            var assignment = await _context.TenantUnitAssignments
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.EndDate == null);
            if (assignment == null) return null;

            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == assignment.UnitId && !u.IsDeleted);
            if (unit == null) return null;

            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == unit.PropertyId && !p.IsDeleted);
            if (property == null) return null;

            return new UnitPropertyInfoDTO
            {
                UnitId = unit.Id,
                UnitNumber = unit.UnitNumber,
                Bedrooms = unit.Bedrooms,
                Bathrooms = unit.Bathrooms,
                SquareFeet = unit.SquareFeet,
                BillingMode = unit.BillingMode,
                PropertyId = property.Id,
                PropertyName = property.Name,
                PropertyAddress = property.Address,
                AdminId = property.AdminId,
            };
        }
    }
}
