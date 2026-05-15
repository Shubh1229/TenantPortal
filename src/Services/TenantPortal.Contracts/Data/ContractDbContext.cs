using Microsoft.EntityFrameworkCore;
using TenantPortal.Contracts.Models;

namespace TenantPortal.Contracts.Data
{
    public class ContractDbContext : DbContext
    {
        public ContractDbContext(DbContextOptions<ContractDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contract>().HasIndex(c => c.BlobStoragePath).IsUnique();
        }
        public DbSet<Contract> Contracts { get; set; }
    }
}
