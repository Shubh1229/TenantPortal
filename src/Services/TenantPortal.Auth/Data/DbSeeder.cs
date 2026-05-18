using Microsoft.EntityFrameworkCore;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Models;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Auth.Data
{
    /// <summary>
    /// Seeds the database with the Super Admin account on first run.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            AuthDbContext context,
            ISecretsProvider secretsProvider,
            ITotpEncryptionService totpEncryption)
        {
            if (await context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
                return;

            var email = await secretsProvider.GetSecretAsync(SecretKeys.SuperAdminEmail);
            var password = await secretsProvider.GetSecretAsync(SecretKeys.SuperAdminPassword);
            var totpSecret = await secretsProvider.GetSecretAsync(SecretKeys.SuperAdminTotpSecret);

            var superAdmin = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                TotpSecret = totpEncryption.Encrypt(totpSecret),
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
            Console.WriteLine("TOTP secret loaded from Key Vault — scan it into your authenticator if not already done.");
            Console.WriteLine("==========================");
        }
    }
}
