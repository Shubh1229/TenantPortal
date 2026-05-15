using Microsoft.EntityFrameworkCore;
using TenantPortal.Notifications.Models;


namespace TenantPortal.Notifications.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });
            modelBuilder.Entity<ReminderSetting>()
                .HasIndex(r => r.UserId);
            modelBuilder.Entity<NotificationPreference>()
                .HasIndex(np => np.UserId)
                .IsUnique();
        }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<ReminderSetting> ReminderSettings { get; set; }
    }
}
