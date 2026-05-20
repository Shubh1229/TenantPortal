using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using TenantPortal.Auth.Data;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Models;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Auth.Services
{
    /// <inheritdoc cref="IAuthService"/>
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly ITotpService _totpService;
        private readonly ITotpEncryptionService _totpEncryption;
        private readonly AuthDbContext _context;
        private readonly ISecretsProvider _secretsProvider;
        private readonly INotificationsGrpcClient _notificationsGrpc;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        // Holds temp tokens issued after password validation, pending TOTP verification.
        // Static + ConcurrentDictionary so multiple concurrent login attempts don't race.
        // Tokens are single-use: removed from the store the moment TOTP is validated.
        private static readonly ConcurrentDictionary<string, Guid> _tempTokenStore = new();

        public AuthService(
            IJwtService jwtService,
            ITotpService totpService,
            ITotpEncryptionService totpEncryption,
            AuthDbContext context,
            ISecretsProvider secretsProvider,
            INotificationsGrpcClient notificationsGrpc,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _jwtService = jwtService;
            _totpService = totpService;
            _totpEncryption = totpEncryption;
            _context = context;
            _secretsProvider = secretsProvider;
            _notificationsGrpc = notificationsGrpc;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc/>
        public async Task<string?> LoginAsync(string email, string password)
        {
            email = email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Issue a short-lived opaque temp token that ties this step to the next (TOTP).
            var tempToken = _jwtService.GenerateRefreshToken();
            _tempTokenStore[tempToken] = user.Id;
            return tempToken;
        }

        /// <inheritdoc/>
        public async Task<LoginResponseDTO?> ValidateTotpAsync(string tempToken, string totpCode)
        {
            // TryRemove is atomic — prevents the same temp token being used twice
            if (!_tempTokenStore.TryRemove(tempToken, out var userId))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted);

            if (user == null || !_totpService.ValidateTotpToken(_totpEncryption.Decrypt(user.TotpSecret), totpCode))
                return null;

            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store the hash, never the plain-text token
            user.RefreshTokenHash = HashToken(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new LoginResponseDTO
            {
                AccessToken = _jwtService.CreateAccessToken(user),
                RefreshToken = refreshToken
            };
        }

        /// <inheritdoc/>
        public async Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.RefreshTokenHash == hash &&
                u.RefreshTokenExpiresAt > DateTime.UtcNow &&
                u.IsActive &&
                !u.IsDeleted);

            if (user == null)
                return null;

            // Rotate: replace the stored hash with a new token's hash on every use
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshTokenHash = HashToken(newRefreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new LoginResponseDTO
            {
                AccessToken = _jwtService.CreateAccessToken(user),
                RefreshToken = newRefreshToken
            };
        }

        /// <inheritdoc/>
        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == hash);
            if (user == null)
                return;

            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<TotpSetupResponseDTO?> RegisterAsync(RegisterRequestDTO request)
        {
            var inviteTokenHash = HashToken(request.InviteToken);
            var inviteRecord = await _context.InviteTokens
                .FirstOrDefaultAsync(t => t.TokenHash == inviteTokenHash);

            if (inviteRecord == null || inviteRecord.Used || inviteRecord.ExpiresAt < DateTime.UtcNow)
                return null;

            var totpSecret = _totpService.GenerateSecret();

            // Check for valid upgrade path (Tester→Admin/Tenant, Tenant→Admin)
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == inviteRecord.Email && !u.IsDeleted);

            if (existingUser != null)
            {
                bool isValidUpgrade =
                    (!existingUser.IsActive && existingUser.Role == inviteRecord.Role) || // activate placeholder
                    (existingUser.Role == UserRole.Tester && inviteRecord.Role == UserRole.Admin) ||
                    (existingUser.Role == UserRole.Tester && inviteRecord.Role == UserRole.Tenant) ||
                    (existingUser.Role == UserRole.Tenant && inviteRecord.Role == UserRole.Admin);

                if (!isValidUpgrade) return null;

                // Activate placeholder or upgrade role. Update InvitedBy to the actual inviter
                // (re-invite via a different Admin should re-attribute the account).
                existingUser.Role = inviteRecord.Role;
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                existingUser.TotpSecret = _totpEncryption.Encrypt(totpSecret);
                existingUser.IsActive = true;
                existingUser.InvitedBy = inviteRecord.CreatedBy;
                existingUser.UpdatedAt = DateTime.UtcNow;

                inviteRecord.Used = true;
                await _context.SaveChangesAsync();

                if (inviteRecord.Role == UserRole.Tester)
                    _ = SeedTesterDataAsync(existingUser.Id);

                return new TotpSetupResponseDTO
                {
                    ManualEntryKey = totpSecret,
                    QrCode = _totpService.GenerateQrCode(totpSecret, inviteRecord.Email)
                };
            }

            // Normal registration — new user
            var newUserId = Guid.NewGuid();
            await _context.Users.AddAsync(new User
            {
                Id = newUserId,
                Role = inviteRecord.Role,
                Email = inviteRecord.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                TotpSecret = _totpEncryption.Encrypt(totpSecret),
                IsActive = true,
                IsDeleted = false,
                InvitedBy = inviteRecord.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            inviteRecord.Used = true;
            await _context.SaveChangesAsync();

            if (inviteRecord.Role == UserRole.Tester)
                _ = SeedTesterDataAsync(newUserId);

            return new TotpSetupResponseDTO
            {
                ManualEntryKey = totpSecret,
                QrCode = _totpService.GenerateQrCode(totpSecret, inviteRecord.Email)
            };
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string? Error)> SendInviteAsync(InviteRequestDTO request, Guid createdBy)
        {
            request.Email = request.Email.Trim().ToLowerInvariant();

            // Block if a valid (unused, unexpired) invite already exists for this email
            var hasPendingInvite = await _context.InviteTokens.AnyAsync(t =>
                t.Email == request.Email && !t.Used && t.ExpiresAt > DateTime.UtcNow);
            if (hasPendingInvite)
                return (false, "An invite is already pending for this email address.");

            // Check if the email belongs to an existing user and enforce upgrade rules
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

            if (existingUser != null)
            {
                // Allowed upgrade paths: re-invite pending placeholder, Tester→Admin/Tenant, Tenant→Admin
                bool isValidUpgrade =
                    (!existingUser.IsActive && existingUser.Role == request.Role) || // re-invite placeholder
                    (existingUser.Role == UserRole.Tester && request.Role == UserRole.Admin) ||
                    (existingUser.Role == UserRole.Tester && request.Role == UserRole.Tenant) ||
                    (existingUser.Role == UserRole.Tenant && request.Role == UserRole.Admin);

                if (!isValidUpgrade)
                {
                    return (existingUser.IsActive, existingUser.Role) switch
                    {
                        (_, UserRole.SuperAdmin) or (_, UserRole.Admin) =>
                            (false, "This email is already registered as an Admin."),
                        (true, UserRole.Tenant) =>
                            (false, "This user is already a Tenant. Send an Admin invite to upgrade them."),
                        (true, UserRole.Tester) =>
                            (false, "This user is already a Tester. Send an Admin or Tenant invite to upgrade them."),
                        _ => (false, "This email is already registered in the system.")
                    };
                }
            }

            var creator = await _context.Users.FirstOrDefaultAsync(u => u.Id == createdBy && !u.IsDeleted);
            if (creator == null || creator.Role == UserRole.Tenant)
                return (false, "You do not have permission to send invites.");

            // SaaS tenant limit: Admins on a paid plan can only invite up to MaxTenants tenants.
            if (creator.Role == UserRole.Admin && creator.MaxTenants.HasValue &&
                request.Role == UserRole.Tenant)
            {
                var activeTenantCount = await _context.Users.CountAsync(u =>
                    u.InvitedBy == createdBy &&
                    u.Role == UserRole.Tenant &&
                    u.IsActive &&
                    !u.IsDeleted);

                if (activeTenantCount >= creator.MaxTenants.Value)
                    return (false, "You have reached your maximum tenant limit for your current plan.");
            }

            var plainToken = _jwtService.GenerateRefreshToken();
            await _context.InviteTokens.AddAsync(new InviteToken
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Role = request.Role,
                TokenHash = HashToken(plainToken),
                ExpiresAt = DateTime.UtcNow.AddDays(2),
                Used = false,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            });

            // Create an inactive placeholder so the invitee appears immediately in the
            // inviter's user list as "pending". On registration the placeholder is activated
            // with the real password and TOTP — no new row is inserted.
            if (existingUser == null)
            {
                await _context.Users.AddAsync(new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    Role = request.Role,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    TotpSecret = _totpEncryption.Encrypt(_totpService.GenerateSecret()),
                    IsActive = false,
                    IsDeleted = false,
                    InvitedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // Fire invite email via gRPC — failure is non-fatal; the invite row is already persisted.
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            await _notificationsGrpc.SendInviteEmailAsync(
                request.Email, plainToken, request.Role.ToString(), frontendBaseUrl);

            return (true, null);
        }

        // Fire-and-forget: seeds demo data in Transactions and Contracts services after Tester registration.
        // Failures are swallowed — seeding is non-fatal and registration is already committed.
        private async Task SeedTesterDataAsync(Guid tenantId)
        {
            var txBase = _configuration["Services:TransactionsUrl"] ?? "http://transactions:8080";
            var cnBase = _configuration["Services:ContractsUrl"] ?? "http://contracts:8080";
            var payload = JsonContent.Create(new { TenantId = tenantId });

            var client = _httpClientFactory.CreateClient();
            try { await client.PostAsync($"{txBase}/api/transactions/internal/seed-tester", payload); } catch { }
            try { await client.PostAsync($"{cnBase}/api/contracts/internal/seed-tester", JsonContent.Create(new { TenantId = tenantId })); } catch { }
        }

        // Emails allowed to skip TOTP in Development. Hard-coded to prevent this list from
        // ever being driven by user input or configuration in any environment.
        private static readonly HashSet<string> _devAccountEmails = new(StringComparer.OrdinalIgnoreCase)
        {
            "fakeadmin@email.com",
            "faketenant@email.com",
            "faketester@email.com",
        };

        /// <inheritdoc/>
        public async Task<LoginResponseDTO?> DevLoginAsync(string email, string password)
        {
            email = email.Trim().ToLowerInvariant();
            if (!_devAccountEmails.Contains(email))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            var refreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshTokenHash = HashToken(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new LoginResponseDTO
            {
                AccessToken = _jwtService.CreateAccessToken(user),
                RefreshToken = refreshToken
            };
        }

        /// <inheritdoc/>
        public async Task<string?> SwitchRoleAsync(Guid superAdminId, UserRole targetRole)
        {
            var superAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == superAdminId && u.IsActive && !u.IsDeleted);

            if (superAdmin == null) return null;

            // Switching back to SuperAdmin returns a clean, un-switched token
            if (targetRole == UserRole.SuperAdmin)
                return _jwtService.CreateAccessToken(superAdmin);

            return _jwtService.CreateSwitchedAccessToken(
                superAdmin.Id.ToString(), superAdmin.Email, targetRole);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<UserListItemDTO>> GetUsersAsync(UserRole? role, Guid callerId, UserRole callerRole)
        {
            // Include pending (IsActive=false) placeholders so the inviter sees them immediately.
            var query = _context.Users.Where(u => !u.IsDeleted);

            // Admins only see users they directly invited
            if (callerRole == UserRole.Admin)
                query = query.Where(u => u.InvitedBy == callerId);

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            return await query
                .Select(u => new UserListItemDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive
                })
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<UserProfileDTO?> GetUserProfileAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return null;

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            var notifEmails = await _context.UserNotificationEmails
                .Where(e => e.UserId == userId)
                .Select(e => new NotificationEmailDTO { Id = e.Id, Email = e.Email })
                .ToListAsync();

            return new UserProfileDTO
            {
                Email = user.Email,
                Role = user.Role.ToString(),
                IsProfileComplete = user.IsProfileComplete,
                FirstName = profile?.FirstName,
                LastName = profile?.LastName,
                PhoneNumber = profile?.PhoneNumber,
                EmergencyContactName = profile?.EmergencyContactName,
                EmergencyContactPhone = profile?.EmergencyContactPhone,
                NotificationEmails = notifEmails
            };
        }

        /// <inheritdoc/>
        public async Task<string?> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequestDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return "User not found.";

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                profile = new Models.UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    EmergencyContactName = request.EmergencyContactName?.Trim(),
                    EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.UserProfiles.AddAsync(profile);
            }
            else
            {
                profile.FirstName = request.FirstName.Trim();
                profile.LastName = request.LastName.Trim();
                profile.PhoneNumber = request.PhoneNumber.Trim();
                profile.EmergencyContactName = request.EmergencyContactName?.Trim();
                profile.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
                profile.UpdatedAt = DateTime.UtcNow;
            }

            user.IsProfileComplete = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return null;
        }

        /// <inheritdoc/>
        public async Task<string?> AddNotificationEmailAsync(Guid userId, string email)
        {
            email = email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email)) return "Email cannot be empty.";

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return "User not found.";

            if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                return "This is already your primary email address.";

            var exists = await _context.UserNotificationEmails
                .AnyAsync(e => e.UserId == userId && e.Email == email);
            if (exists) return "This email is already in your notification list.";

            await _context.UserNotificationEmails.AddAsync(new Models.UserNotificationEmail
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Email = email,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteNotificationEmailAsync(Guid userId, Guid emailId)
        {
            var entry = await _context.UserNotificationEmails
                .FirstOrDefaultAsync(e => e.Id == emailId && e.UserId == userId);
            if (entry == null) return false;

            _context.UserNotificationEmails.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<string?> UpdatePrimaryEmailAsync(Guid userId, string newEmail, string currentPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted);
            if (user == null) return "User not found.";

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return "Current password is incorrect.";

            newEmail = newEmail.Trim().ToLowerInvariant();

            var emailTaken = await _context.Users
                .AnyAsync(u => u.Email == newEmail && u.Id != userId && !u.IsDeleted);
            if (emailTaken)
                return "That email address is already in use by another account.";

            user.Email = newEmail;
            user.UpdatedAt = DateTime.UtcNow;
            // Revoke refresh token so the user re-logs in with the new email
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            await _context.SaveChangesAsync();
            return null;
        }

        /// <inheritdoc/>
        public async Task<string?> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted);
            if (user == null) return "User not found.";

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return "Current password is incorrect.";

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            // Revoke refresh token so all sessions must re-authenticate
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            await _context.SaveChangesAsync();
            return null;
        }

        /// <inheritdoc/>
        public async Task<string?> DeleteAccountAsync(Guid userId, string confirmEmail)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return "User not found.";

            if (!string.Equals(user.Email, confirmEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                return "The email address you entered does not match your account email.";

            user.IsDeleted = true;
            user.IsActive = false;
            user.DeletedAt = DateTime.UtcNow;
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return null;
        }

        /// <summary>
        /// SHA-256 hashes a token for safe storage and constant-time comparison.
        /// All token comparisons go through this method — never compare raw token strings.
        /// </summary>
        private static string HashToken(string token) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
