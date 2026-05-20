using Microsoft.EntityFrameworkCore;

namespace TenantPortal.Auth.Data
{
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

            modelBuilder.Entity<Models.UserProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            modelBuilder.Entity<Models.UserNotificationEmail>()
                .HasIndex(e => new { e.UserId, e.Email })
                .IsUnique();

            modelBuilder.Entity<Models.UserNotificationEmail>()
                .Property(e => e.Email)
                .HasMaxLength(256);
        }

        public DbSet<Models.User> Users { get; set; }
        public DbSet<Models.InviteToken> InviteTokens { get; set; }
        public DbSet<Models.UserProfile> UserProfiles { get; set; }
        public DbSet<Models.UserNotificationEmail> UserNotificationEmails { get; set; }
    }
}
