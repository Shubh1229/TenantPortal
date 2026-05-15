using Microsoft.EntityFrameworkCore;
using TenantPortal.Contracts.Models;

namespace TenantPortal.Contracts.Data
{
    /// <summary>
    /// EF Core database context for the Contracts service.
    /// Manages <see cref="Contract"/> metadata; actual PDF files are stored in Azure Blob Storage.
    /// </summary>
    public class ContractDbContext : DbContext
    {
        public ContractDbContext(DbContextOptions<ContractDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // BlobStoragePath uniqueness prevents two records pointing at the same blob
            modelBuilder.Entity<Contract>().HasIndex(c => c.BlobStoragePath).IsUnique();
        }

        /// <summary>Contract metadata records. The referenced PDFs live in Azure Blob Storage.</summary>
        public DbSet<Contract> Contracts { get; set; }
    }
}
