using Microsoft.EntityFrameworkCore;
using TenantPortal.Notifications.Models;

namespace TenantPortal.Notifications.Data
{
    /// <summary>
    /// EF Core database context for the Notifications service.
    /// Manages in-app notifications, reminder settings, and notification preferences.
    /// </summary>
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indexes from the spec's recommended index list
            modelBuilder.Entity<Notification>().HasIndex(n => n.UserId);
            modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });
            modelBuilder.Entity<ReminderSetting>().HasIndex(r => r.UserId);

            // One preference row per user
            modelBuilder.Entity<NotificationPreference>().HasIndex(np => np.UserId).IsUnique();
        }

        /// <summary>In-app notification records for all users.</summary>
        public DbSet<Notification> Notifications { get; set; }

        /// <summary>Per-user email notification preferences.</summary>
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }

        /// <summary>Configurable rent reminder settings per user.</summary>
        public DbSet<ReminderSetting> ReminderSettings { get; set; }
    }
}
