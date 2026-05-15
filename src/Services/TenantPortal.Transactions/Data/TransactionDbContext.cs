using Microsoft.EntityFrameworkCore;
using TenantPortal.Transactions.Models;

namespace TenantPortal.Transactions.Data
{
    /// <summary>
    /// EF Core database context for the Transactions service.
    /// Manages properties, units, rent schedules, transactions, and tenant-unit assignments.
    /// </summary>
    public class TransactionDbContext : DbContext
    {
        public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indexes defined per the spec's recommended index list
            modelBuilder.Entity<Transaction>().HasIndex(t => t.TenantId);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.Status);
            modelBuilder.Entity<Transaction>().HasIndex(t => t.DueDate);
            modelBuilder.Entity<TenantUnitAssignment>().HasIndex(t => t.TenantId);
            modelBuilder.Entity<TenantUnitAssignment>().HasIndex(t => t.UnitId);

            // Supports the per-admin scope query: filter Properties by AdminId, then join to Units/Transactions
            modelBuilder.Entity<Property>().HasIndex(p => p.AdminId);
        }

        /// <summary>Rental properties.</summary>
        public DbSet<Property> Properties { get; set; }

        /// <summary>All transaction records across all tenants.</summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>Recurring rent schedules per tenant.</summary>
        public DbSet<RentSchedule> RentSchedules { get; set; }

        /// <summary>Historical and current tenant-to-unit assignments.</summary>
        public DbSet<TenantUnitAssignment> TenantUnitAssignments { get; set; }

        /// <summary>Individual rentable units within properties.</summary>
        public DbSet<Unit> Units { get; set; }
    }
}
