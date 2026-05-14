using TenantPortal.Auth.Models;
using TenantPortal.Shared.Interfaces;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Exceptions;

namespace TenantPortal.Auth.Services
{
    public class JwtService : IJwtService
    {
        private readonly ISecretsProvider _secretsProvider;
        public JwtService(ISecretsProvider secretsProvider)
        {
            _secretsProvider = secretsProvider;
        }

        public string CreateAccessToken(User user) 
        {
            string? jwtSecretKey = _secretsProvider.GetSecretAsync(SecretKeys.JwtSigningKey).GetAwaiter().GetResult();
            if (jwtSecretKey == null)
            {
                throw new NotFoundException("JWT Secret Key not found");
            }
            List<Claim> claims = new List<Claim>
            {
                new Claim(AppConstants.Claims.UserId, user.Id.ToString()),
                new Claim(AppConstants.Claims.Email, user.Email),
                new Claim(AppConstants.Claims.UserRole, user.Role.ToString())
            };

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: signingCredentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var cryptoRandomNumber = new byte[32];
            RandomNumberGenerator.Fill(cryptoRandomNumber);
            return Convert.ToBase64String(cryptoRandomNumber);
        }

        public Guid? ValidateRefreshToken(string token)
        {
            try
            {
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                if (jwtToken == null)
                {
                    return null;
                }
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == AppConstants.Claims.UserId);
                if (userIdClaim == null)
                {
                    return null;
                }
                Guid result;
                bool guidParsed = Guid.TryParse(userIdClaim.Value, out result);
                if (!guidParsed)
                {
                    return null;
                }
                return result;
            } catch (Exception ex)
            {
                return null;
            }
        }
    }
}
