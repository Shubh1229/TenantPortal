using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TenantPortal.Auth.Data;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Models;
using TenantPortal.Shared.Interfaces;
using System.Security.Cryptography;
using TenantPortal.Shared.Enums;
using TenantPortal.Auth.Interfaces;

namespace TenantPortal.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly ITotpService _totpService;
        private readonly AuthDbContext _context;
        private readonly ISecretsProvider _secretsProvider;
        private static readonly Dictionary<string, Guid> _tempTokenStore = new();

        public AuthService(IJwtService jwtService, ITotpService totpService, AuthDbContext context, ISecretsProvider secretsProvider)
        {
            _jwtService = jwtService;
            _totpService = totpService;
            _context = context;
            _secretsProvider = secretsProvider;
        }
        public async Task<string?> LoginAsync(string email, string password)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return null;
            }
            bool passwordMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!passwordMatch)
            {
                return null;
            }

            var tempToken = _jwtService.GenerateRefreshToken();
            _tempTokenStore[tempToken] = user.Id;
            return tempToken;
        }

        public async Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken)
        {
            Guid? userId = _jwtService.ValidateRefreshToken(refreshToken);
            if (userId == null)
            {
                return null;
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            return new LoginResponseDTO
            {
                AccessToken = _jwtService.CreateAccessToken(user),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };
        }

        public async Task<TotpSetupResponseDTO?> RegisterAsync(RegisterRequestDTO request)
        {
            var inviteToken = request.InviteToken;
            var inviteTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(inviteToken)));
            var inviteRecord = await _context.InviteTokens.FirstOrDefaultAsync(t => t.TokenHash == inviteTokenHash);
            if (inviteRecord == null || inviteRecord.Used || inviteRecord.ExpiresAt < DateTime.UtcNow)
            {
                return null;
            }
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == inviteRecord.Email);
            if (user != null)
            {
                return null;
            }
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var newTotpSecret = _totpService.GenerateSecret();
            await _context.Users.AddAsync(new User
            {
                Id = Guid.NewGuid(),
                Role = inviteRecord.Role,
                Email = inviteRecord.Email,
                PasswordHash = passwordHash,
                TotpSecret = newTotpSecret
            });
            inviteRecord.Used = true;
            await _context.SaveChangesAsync();
            return new TotpSetupResponseDTO
            {
                ManualEntryKey = newTotpSecret,
                QrCode = _totpService.GenerateQrCode(newTotpSecret, inviteRecord.Email)
            };
        }

        public Task RevokeRefreshTokenAsync(string refreshToken)
        {
            Guid? userId = _jwtService.ValidateRefreshToken(refreshToken);
            if (userId != null)
            {
                foreach (string key in _tempTokenStore.Keys)
                {
                    Guid value = _tempTokenStore[key];
                    if (value == userId)
                    {
                        _tempTokenStore.Remove(key);
                        break;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public async Task<bool> SendInviteAsync(InviteRequestDTO request, Guid createdBy)
        {
            var invitedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (invitedUser != null)
            {
                return false;
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == createdBy);
            if (user == null || user.Role == UserRole.Tenant)
            {
                return false;
            }

            var inviteToken = _jwtService.GenerateRefreshToken();
            var inviteTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(inviteToken)));
            var inviteRecord = new InviteToken
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Role = request.Role,
                TokenHash = inviteTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(2),
                Used = false,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
            await _context.InviteTokens.AddAsync(inviteRecord);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LoginResponseDTO?> ValidateTotpAsync(string tempToken, string totpCode)
        {
            if(!_tempTokenStore.TryGetValue(tempToken, out var userID))
            {
                return null;
            }
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userID);
            if (user == null)
            {
                return null;
            }
            var totpValid = _totpService.ValidateTotpToken(user.TotpSecret, totpCode);
            if (!totpValid)
            {
                return null;
            }
            _tempTokenStore.Remove(tempToken);
            LoginResponseDTO response = new LoginResponseDTO
            {
                AccessToken = _jwtService.CreateAccessToken(user),
                RefreshToken = _jwtService.GenerateRefreshToken()
            };
            return response;
        }
    }
}
