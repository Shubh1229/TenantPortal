using Microsoft.EntityFrameworkCore;
using TenantPortal.Shared.Enums;
using TenantPortal.Transactions.Data;

namespace TenantPortal.Transactions.Services
{
    public class OverduePaymentJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OverduePaymentJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextMidnight = now.Date.AddDays(1);
                var delay = nextMidnight - now;
                await Task.Delay(delay, stoppingToken);
                await RunOverdueCheckAsync();
            }
        }

        private async Task RunOverdueCheckAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();

            var overdueTransactions = await context.Transactions
                .Where(t => !t.IsDeleted
                    && t.DueDate < DateTime.UtcNow
                    && t.Status != TransactionStatus.Confirmed
                    && t.Status != TransactionStatus.Pending
                    && t.Status != TransactionStatus.Overdue)
                .ToListAsync();
            foreach ( var transaction in overdueTransactions )
            {
                transaction.Status = TransactionStatus.Overdue;
                transaction.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
    }
}
