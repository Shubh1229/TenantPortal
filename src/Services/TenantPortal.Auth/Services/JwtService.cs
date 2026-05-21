using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Models;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Auth.Services
{
    /// <inheritdoc cref="IJwtService"/>
    public class JwtService : IJwtService
    {
        private readonly ISecretsProvider _secretsProvider;

        public JwtService(ISecretsProvider secretsProvider)
        {
            _secretsProvider = secretsProvider;
        }

        /// <inheritdoc/>
        public string CreateAccessToken(User user)
        {
            var signingKey = _secretsProvider.GetSecretAsync(SecretKeys.JwtSigningKey).GetAwaiter().GetResult();

            var claims = new List<Claim>
            {
                new(AppConstants.Claims.UserId, user.Id.ToString()),
                new(AppConstants.Claims.Email, user.Email),
                new(AppConstants.Claims.UserRole, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <inheritdoc/>
        public string CreateSwitchedAccessToken(string superAdminId, string email, UserRole targetRole)
        {
            var signingKey = _secretsProvider.GetSecretAsync(SecretKeys.JwtSigningKey).GetAwaiter().GetResult();

            var claims = new List<Claim>
            {
                new(AppConstants.Claims.UserId, superAdminId),
                new(AppConstants.Claims.Email, email),
                new(AppConstants.Claims.UserRole, targetRole.ToString()),
                new(AppConstants.Claims.IsSuperAdminSwitched, "true")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <inheritdoc/>
        public string GenerateRefreshToken()
        {
            // 32 random bytes = 256 bits of entropy, sufficient for a long-lived opaque token
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
