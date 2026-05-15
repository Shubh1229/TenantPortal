using Microsoft.EntityFrameworkCore;
using TenantPortal.Auth.Models;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Helpers;
using TenantPortal.Shared.Constants;

namespace TenantPortal.Auth.Data
{
    /// <summary>
    /// Seeds the database with the Super Admin account on first run.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(AuthDbContext context)
        {
            // Only seed if no super admin exists
            if (await context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
                return;

            var secretsProvider = new LocalSecretsProvider();
            var email = await secretsProvider.GetSecretAsync(SecretKeys.SuperAdminEmail);
            var password = await secretsProvider.GetSecretAsync(SecretKeys.SuperAdminPassword);

            var totpService = new TenantPortal.Auth.Services.TotpService();
            var totpSecret = totpService.GenerateSecret();

            var superAdmin = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                TotpSecret = totpSecret,
                Role = UserRole.SuperAdmin,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await context.Users.AddAsync(superAdmin);
            await context.SaveChangesAsync();

            Console.WriteLine("=== SUPER ADMIN SEEDED ===");
            Console.WriteLine($"Email: {email}");
            Console.WriteLine($"TOTP Secret: {totpSecret}");
            Console.WriteLine("Scan the TOTP secret into your authenticator app.");
            Console.WriteLine("==========================");
        }
    }
}