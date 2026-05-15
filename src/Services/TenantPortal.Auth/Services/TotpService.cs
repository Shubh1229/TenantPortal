using OtpNet;
using QRCoder;
using TenantPortal.Auth.Interfaces;

namespace TenantPortal.Auth.Services
{
    /// <inheritdoc cref="ITotpService"/>
    public class TotpService : ITotpService
    {
        /// <inheritdoc/>
        public string GenerateSecret()
        {
            // SHA-256 key size gives 32 bytes (256 bits) of TOTP secret entropy
            var randomKey = KeyGeneration.GenerateRandomKey(OtpHashMode.Sha256);
            return Base32Encoding.ToString(randomKey);
        }

        /// <inheritdoc/>
        public string GenerateQrCode(string secret, string email)
        {
            var uri = $"otpauth://totp/TenantPortal:{email}?secret={secret}&issuer=TenantPortal";
            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            return Convert.ToBase64String(qrCode.GetGraphic(20));
        }

        /// <inheritdoc/>
        public bool ValidateTotpToken(string secret, string token)
        {
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            // VerificationWindow(2, 2) allows ±2 time steps (±60 seconds) to account for clock drift
            return totp.VerifyTotp(token, out _, new VerificationWindow(2, 2));
        }
    }
}
