using Microsoft.EntityFrameworkCore;
using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.Data;

namespace TenantPortal.Transactions.Services
{
    /// <summary>
    /// Background service that runs once per day at midnight UTC.
    /// Scans all non-deleted transactions with a past due date and no confirmed or pending payment,
    /// then transitions them to <see cref="TransactionStatus.Overdue"/>.
    /// </summary>
    public class OverduePaymentJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OverduePaymentJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Sleep until the next midnight UTC, then run the overdue check
                var now = DateTime.UtcNow;
                var delay = now.Date.AddDays(1) - now;
                await Task.Delay(delay, stoppingToken);
                await RunOverdueCheckAsync();
            }
        }

        private async Task RunOverdueCheckAsync()
        {
            // Use a scope because DbContext is scoped and BackgroundService is singleton
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();

            var overdueTransactions = await context.Transactions
                .Where(t => !t.IsDeleted
                    && t.DueDate < DateTime.UtcNow
                    && t.Status != TransactionStatus.Confirmed
                    && t.Status != TransactionStatus.Pending
                    && t.Status != TransactionStatus.Overdue)
                .ToListAsync();

            foreach (var transaction in overdueTransactions)
            {
                transaction.Status = TransactionStatus.Overdue;
                transaction.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
    }
}
