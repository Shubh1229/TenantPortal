using Microsoft.EntityFrameworkCore;
using TenantPortal.Transactions.Models;

namespace TenantPortal.Transactions.Data
{
    public class TransactionDbContext : DbContext
    {
        public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.TenantId);
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Status);
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.DueDate);
            modelBuilder.Entity<TenantUnitAssignment>()
                .HasIndex(t => t.TenantId);
            modelBuilder.Entity<TenantUnitAssignment>()
                .HasIndex(t => t.UnitId);
        }

        public DbSet<Property> Properties { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<RentSchedule> RentSchedules { get; set; }
        public DbSet<TenantUnitAssignment> TenantUnitAssignments { get; set; }
        public DbSet<Unit> Units { get; set; }
    }
}
