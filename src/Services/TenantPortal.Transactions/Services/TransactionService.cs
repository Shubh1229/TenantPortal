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
            if (role == UserRole.Tenant)
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
            if (role == UserRole.Tenant)
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
                    PaidDate = request.PaidDate,
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
    }
}
