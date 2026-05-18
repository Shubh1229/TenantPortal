using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
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
            IConfiguration configuration)
        {
            _jwtService = jwtService;
            _totpService = totpService;
            _totpEncryption = totpEncryption;
            _context = context;
            _secretsProvider = secretsProvider;
            _notificationsGrpc = notificationsGrpc;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public async Task<string?> LoginAsync(string email, string password)
        {
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
                    (existingUser.Role == UserRole.Tester && inviteRecord.Role == UserRole.Admin) ||
                    (existingUser.Role == UserRole.Tester && inviteRecord.Role == UserRole.Tenant) ||
                    (existingUser.Role == UserRole.Tenant && inviteRecord.Role == UserRole.Admin);

                if (!isValidUpgrade) return null;

                // Upgrade: update role, password, and TOTP. Old sessions expire naturally.
                existingUser.Role = inviteRecord.Role;
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                existingUser.TotpSecret = _totpEncryption.Encrypt(totpSecret);
                existingUser.UpdatedAt = DateTime.UtcNow;

                inviteRecord.Used = true;
                await _context.SaveChangesAsync();

                return new TotpSetupResponseDTO
                {
                    ManualEntryKey = totpSecret,
                    QrCode = _totpService.GenerateQrCode(totpSecret, inviteRecord.Email)
                };
            }

            // Normal registration — new user
            await _context.Users.AddAsync(new User
            {
                Id = Guid.NewGuid(),
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

            return new TotpSetupResponseDTO
            {
                ManualEntryKey = totpSecret,
                QrCode = _totpService.GenerateQrCode(totpSecret, inviteRecord.Email)
            };
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string? Error)> SendInviteAsync(InviteRequestDTO request, Guid createdBy)
        {
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
                // Allowed upgrade paths: Tester→Admin, Tester→Tenant, Tenant→Admin
                bool isValidUpgrade =
                    (existingUser.Role == UserRole.Tester && request.Role == UserRole.Admin) ||
                    (existingUser.Role == UserRole.Tester && request.Role == UserRole.Tenant) ||
                    (existingUser.Role == UserRole.Tenant && request.Role == UserRole.Admin);

                if (!isValidUpgrade)
                {
                    return (existingUser.Role) switch
                    {
                        UserRole.SuperAdmin or UserRole.Admin =>
                            (false, "This email is already registered as an Admin."),
                        UserRole.Tenant =>
                            (false, "This user is already a Tenant. Send an Admin invite to upgrade them."),
                        UserRole.Tester =>
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

            await _context.SaveChangesAsync();

            // Fire invite email via gRPC — failure is non-fatal; the invite row is already persisted.
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            await _notificationsGrpc.SendInviteEmailAsync(
                request.Email, plainToken, request.Role.ToString(), frontendBaseUrl);

            return (true, null);
        }

        /// <summary>
        /// SHA-256 hashes a token for safe storage and constant-time comparison.
        /// All token comparisons go through this method — never compare raw token strings.
        /// </summary>
        private static string HashToken(string token) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
