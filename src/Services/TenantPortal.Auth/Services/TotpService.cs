using OtpNet;
using QRCoder;


namespace TenantPortal.Auth.Services
{
    public class TotpService : ITotpService
    {
        public string GenerateQrCode(string secret, string email)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qRCodeData = qrGenerator.CreateQrCode($"otpauth://totp/TenantPortal:{email}?secret={secret}&issuer=TenantPortal", QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qRCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }

        public string GenerateSecret()
        {
            var randomKey = OtpNet.KeyGeneration.GenerateRandomKey(OtpNet.OtpHashMode.Sha256);
            return OtpNet.Base32Encoding.ToString(randomKey);
        }

        public bool ValidateTotpToken(string secret, string token)
        {
            var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret));
            return totp.VerifyTotp(token, out long timeStepMatched, new OtpNet.VerificationWindow(2, 2));
        }
    }
}
