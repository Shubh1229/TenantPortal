using Microsoft.EntityFrameworkCore;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Models;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Auth.Data
{
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
                IsProfileComplete = true,
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

        public static async Task SeedDevAccountsAsync(
            AuthDbContext context,
            ITotpEncryptionService totpEncryption)
        {
            var devAccounts = new[]
            {
                (Email: "fakeadmin@email.com",   Password: "DevAdmin123!",   Role: UserRole.Admin),
                (Email: "faketenant@email.com",  Password: "DevTenant123!",  Role: UserRole.Tenant),
                (Email: "faketester@email.com",  Password: "DevTester123!",  Role: UserRole.Tester),
            };

            // Fixup for already-seeded DBs: ensure InvitedBy is linked correctly.
            var existingFakeAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == "fakeadmin@email.com");
            var existingFakeTenant = await context.Users.FirstOrDefaultAsync(u => u.Email == "faketenant@email.com");
            if (existingFakeAdmin != null && existingFakeTenant != null && existingFakeTenant.InvitedBy != existingFakeAdmin.Id)
            {
                existingFakeTenant.InvitedBy = existingFakeAdmin.Id;
                await context.SaveChangesAsync();
            }

            Guid? fakeAdminId = existingFakeAdmin?.Id;

            // Fake profile data for each dev account
            var fakeProfiles = new Dictionary<string, (string First, string Last, string Phone, string ECName, string ECPhone)>
            {
                ["fakeadmin@email.com"]   = ("Alex",   "Singh",   "412-555-0101", "Priya Singh",  "412-555-0102"),
                ["faketenant@email.com"]  = ("Jordan", "Rivera",  "412-555-0201", "Casey Rivera", "412-555-0202"),
                ["faketester@email.com"]  = ("Morgan", "Chen",    "412-555-0301", "Taylor Chen",  "412-555-0302"),
            };

            bool anyCreated = false;
            foreach (var (email, password, role) in devAccounts)
            {
                var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser != null)
                {
                    // Backfill profile and IsProfileComplete for existing dev accounts
                    if (!await context.UserProfiles.AnyAsync(p => p.UserId == existingUser.Id))
                    {
                        var (first, last, phone, ecn, ecp) = fakeProfiles[email];
                        await context.UserProfiles.AddAsync(new UserProfile
                        {
                            Id = Guid.NewGuid(),
                            UserId = existingUser.Id,
                            FirstName = first,
                            LastName = last,
                            PhoneNumber = phone,
                            EmergencyContactName = ecn,
                            EmergencyContactPhone = ecp,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        });
                        existingUser.IsProfileComplete = true;
                        existingUser.UpdatedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                    }
                    continue;
                }

                var newId = Guid.NewGuid();

                if (role == UserRole.Admin)
                    fakeAdminId = newId;

                var invitedBy = (role == UserRole.Tenant) ? fakeAdminId : (Guid?)null;

                await context.Users.AddAsync(new User
                {
                    Id = newId,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    TotpSecret = totpEncryption.Encrypt(Guid.NewGuid().ToString("N")),
                    Role = role,
                    IsActive = true,
                    IsDeleted = false,
                    IsProfileComplete = true,
                    InvitedBy = invitedBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });

                var (fn, ln, ph, ecName, ecPhone) = fakeProfiles[email];
                await context.UserProfiles.AddAsync(new UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = newId,
                    FirstName = fn,
                    LastName = ln,
                    PhoneNumber = ph,
                    EmergencyContactName = ecName,
                    EmergencyContactPhone = ecPhone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });

                anyCreated = true;
            }

            if (anyCreated)
            {
                await context.SaveChangesAsync();
                Console.WriteLine("=== DEV ACCOUNTS SEEDED ===");
                Console.WriteLine("  fakeadmin@email.com   / DevAdmin123!");
                Console.WriteLine("  faketenant@email.com  / DevTenant123!");
                Console.WriteLine("  faketester@email.com  / DevTester123!");
                Console.WriteLine("  (TOTP bypassed via /api/auth/dev-login)");
                Console.WriteLine("===========================");
            }
        }
    }
}
