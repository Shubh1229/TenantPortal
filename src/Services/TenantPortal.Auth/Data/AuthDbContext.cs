using Microsoft.EntityFrameworkCore;

namespace TenantPortal.Auth.Data
{
    /// <summary>
    /// EF Core database context for the Auth service.
    /// Manages <see cref="Models.User"/> and <see cref="Models.InviteToken"/> tables.
    /// </summary>
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Models.InviteToken>()
                .HasIndex(t => t.TokenHash)
                .IsUnique();

            modelBuilder.Entity<Models.User>().Property(u => u.Email).HasMaxLength(256);
            modelBuilder.Entity<Models.User>().Property(u => u.PasswordHash).HasMaxLength(512);
            modelBuilder.Entity<Models.User>().Property(u => u.TotpSecret).HasMaxLength(256);

            modelBuilder.Entity<Models.InviteToken>().Property(t => t.TokenHash).HasMaxLength(512);
            modelBuilder.Entity<Models.InviteToken>().Property(t => t.Email).HasMaxLength(256);
        }

        /// <summary>Registered users across all roles.</summary>
        public DbSet<Models.User> Users { get; set; }

        /// <summary>Pending account invitations, including consumed and expired ones.</summary>
        public DbSet<Models.InviteToken> InviteTokens { get; set; }
    }
}
