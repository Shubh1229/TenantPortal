using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TenantPortal.Auth.Data;
using TenantPortal.Auth.DTOs;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Models;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Interfaces;

namespace TenantPortal.Auth.Services
{
    public class SystemTestRunner
    {
        private readonly AuthDbContext _context;
        private readonly ISecretsProvider _secrets;
        private readonly INotificationsGrpcClient _grpcClient;
        private readonly ITotpEncryptionService _totpEncryption;
        private readonly IJwtService _jwtService;
        private readonly ILogger<SystemTestRunner> _logger;

        public SystemTestRunner(
            AuthDbContext context,
            ISecretsProvider secrets,
            INotificationsGrpcClient grpcClient,
            ITotpEncryptionService totpEncryption,
            IJwtService jwtService,
            ILogger<SystemTestRunner> logger)
        {
            _context = context;
            _secrets = secrets;
            _grpcClient = grpcClient;
            _totpEncryption = totpEncryption;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<TestSuiteResult> RunAllAsync()
        {
            var suiteStart = Stopwatch.StartNew();
            _logger.LogInformation("[SystemTest] Starting system test suite");

            var tests = new List<TestResult>
            {
                await RunTest("Auth DB: Connectivity",       "Database",     TestDbConnectivityAsync),
                await RunTest("Auth DB: Migrations",         "Database",     TestDbMigrationsAsync),
                await RunTest("Auth DB: User Statistics",    "Database",     TestUserStatsAsync),
                await RunTest("Auth DB: Invite Tokens",      "Database",     TestInviteTokensAsync),
                await RunTest("Key Vault: JWT Signing Key",  "Azure",        TestKvJwtKeyAsync),
                await RunTest("Key Vault: TOTP Enc Key",     "Azure",        TestKvTotpKeyAsync),
                await RunTest("Key Vault: ACS Connection",   "Azure",        TestKvAcsAsync),
                await RunTest("Security: TOTP Encryption",   "Security",     TestTotpRoundTripAsync),
                await RunTest("Security: JWT Generation",    "Security",     TestJwtAsync),
                await RunTest("gRPC: Notifications Channel", "Connectivity", TestGrpcPingAsync),
            };

            var result = new TestSuiteResult
            {
                RunAt = DateTime.UtcNow,
                TotalDurationMs = (int)suiteStart.ElapsedMilliseconds,
                Passed = tests.Count(t => t.Passed),
                Failed = tests.Count(t => !t.Passed),
                Tests = tests,
            };

            _logger.LogInformation("[SystemTest] Suite complete: {Passed}/{Total} passed in {Ms}ms",
                result.Passed, tests.Count, result.TotalDurationMs);

            return result;
        }

        private async Task<TestResult> RunTest(
            string name,
            string category,
            Func<Task<(bool Passed, string Message, List<string> Logs)>> test)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var (passed, message, logs) = await test();
                _logger.LogInformation("[SystemTest] {Status} | {Name} ({Ms}ms) — {Message}",
                    passed ? "PASS" : "FAIL", name, sw.ElapsedMilliseconds, message);
                return new TestResult
                {
                    Name = name, Category = category, Passed = passed,
                    Message = message, Logs = logs, DurationMs = (int)sw.ElapsedMilliseconds,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SystemTest] EXCEPTION | {Name}", name);
                return new TestResult
                {
                    Name = name, Category = category, Passed = false,
                    Message = $"{ex.GetType().Name}: {ex.Message}",
                    Logs = [$"EXCEPTION: {ex}"],
                    DurationMs = (int)sw.ElapsedMilliseconds,
                };
            }
        }

        // ── Database ─────────────────────────────────────────────────────────

        private async Task<(bool, string, List<string>)> TestDbConnectivityAsync()
        {
            var logs = new List<string>();
            logs.Add("Calling Database.CanConnectAsync()...");
            var ok = await _context.Database.CanConnectAsync();
            logs.Add($"Result: {ok}");
            return (ok, ok ? "Auth database is reachable" : "Cannot connect to auth database", logs);
        }

        private async Task<(bool, string, List<string>)> TestDbMigrationsAsync()
        {
            var logs = new List<string>();
            var applied = (await _context.Database.GetAppliedMigrationsAsync()).ToList();
            logs.Add($"Applied migrations: {applied.Count}");
            foreach (var m in applied) logs.Add($"  ✓ {m}");

            var pending = (await _context.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logs.Add($"Pending migrations: {pending.Count}");
                foreach (var m in pending) logs.Add($"  ! {m}");
            }
            else
            {
                logs.Add("No pending migrations.");
            }

            return (pending.Count == 0,
                pending.Count == 0
                    ? $"{applied.Count} applied, none pending"
                    : $"{pending.Count} pending migration(s) — run docker compose up to apply",
                logs);
        }

        private async Task<(bool, string, List<string>)> TestUserStatsAsync()
        {
            var logs = new List<string>();
            var total = await _context.Users.CountAsync(u => !u.IsDeleted);
            logs.Add($"Total active (non-deleted) users: {total}");
            foreach (UserRole role in Enum.GetValues<UserRole>())
            {
                var count = await _context.Users.CountAsync(u => u.Role == role && !u.IsDeleted);
                logs.Add($"  {role,-12}: {count}");
            }
            var inactive = await _context.Users.CountAsync(u => !u.IsActive && !u.IsDeleted);
            logs.Add($"  Inactive (not deleted): {inactive}");
            var deleted = await _context.Users.CountAsync(u => u.IsDeleted);
            if (deleted > 0) logs.Add($"  Soft-deleted: {deleted}");
            return (true, $"{total} active user(s) across all roles", logs);
        }

        private async Task<(bool, string, List<string>)> TestInviteTokensAsync()
        {
            var logs = new List<string>();
            var now = DateTime.UtcNow;
            var pending = await _context.InviteTokens.CountAsync(t => !t.Used && t.ExpiresAt > now);
            var used    = await _context.InviteTokens.CountAsync(t => t.Used);
            var expired = await _context.InviteTokens.CountAsync(t => !t.Used && t.ExpiresAt <= now);
            logs.Add($"Pending  (active):  {pending}");
            logs.Add($"Used:               {used}");
            logs.Add($"Expired (unused):   {expired}");

            if (expired > 0)
                logs.Add($"Note: {expired} expired invite token(s) can be cleaned up from the DB.");

            return (true, $"{pending} pending, {used} used, {expired} expired", logs);
        }

        // ── Azure Key Vault ───────────────────────────────────────────────────

        private async Task<(bool, string, List<string>)> TestKvJwtKeyAsync()
        {
            var logs = new List<string>();
            logs.Add($"Fetching secret key: {SecretKeys.JwtSigningKey}");
            var key = await _secrets.GetSecretAsync(SecretKeys.JwtSigningKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                logs.Add("ERROR: Secret is null or empty.");
                return (false, "JWT signing key missing from Key Vault", logs);
            }
            // The JWT key is used as-is via Encoding.UTF8.GetBytes — it is not Base-64 encoded.
            var bytes = Encoding.UTF8.GetBytes(key);
            logs.Add($"Key length: {bytes.Length} bytes ({bytes.Length * 8} bits)");
            var ok = bytes.Length >= 32;
            logs.Add(ok ? "✓ Meets HS256 minimum (256 bits)" : "✗ Shorter than recommended 256 bits");
            return (ok, $"{bytes.Length * 8}-bit signing key present", logs);
        }

        private async Task<(bool, string, List<string>)> TestKvTotpKeyAsync()
        {
            var logs = new List<string>();
            logs.Add($"Fetching secret key: {SecretKeys.TotpEncryptionKey}");
            var key = await _secrets.GetSecretAsync(SecretKeys.TotpEncryptionKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                logs.Add("ERROR: Secret is null or empty.");
                return (false, "TOTP encryption key missing from Key Vault", logs);
            }
            var bytes = Convert.FromBase64String(key);
            logs.Add($"Key length: {bytes.Length} bytes ({bytes.Length * 8} bits)");
            var ok = bytes.Length == 32;
            logs.Add(ok ? "✓ Exactly 256 bits (AES-256-GCM requirement met)" : $"✗ Expected 32 bytes, got {bytes.Length}");
            return (ok, $"{bytes.Length * 8}-bit TOTP encryption key present", logs);
        }

        private async Task<(bool, string, List<string>)> TestKvAcsAsync()
        {
            var logs = new List<string>();
            logs.Add($"Fetching secret key: {SecretKeys.AzureCommunicationServices}");
            var conn = await _secrets.GetSecretAsync(SecretKeys.AzureCommunicationServices);
            if (string.IsNullOrWhiteSpace(conn))
            {
                logs.Add("ERROR: Secret is null or empty.");
                return (false, "ACS connection string missing from Key Vault", logs);
            }
            logs.Add($"Connection string length: {conn.Length} chars");
            var hasEndpoint = conn.Contains("endpoint=", StringComparison.OrdinalIgnoreCase);
            logs.Add(hasEndpoint ? "✓ Contains 'endpoint=' — format looks valid" : "✗ Unexpected format (no 'endpoint=' found)");
            return (hasEndpoint, hasEndpoint ? "ACS connection string present and valid" : "ACS string present but unexpected format", logs);
        }

        // ── Security ─────────────────────────────────────────────────────────

        private Task<(bool, string, List<string>)> TestTotpRoundTripAsync()
        {
            var logs = new List<string>();
            const string plaintext = "JBSWY3DPEHPK3PXP"; // RFC 6238 test vector
            logs.Add($"Test plaintext: {plaintext}");

            var encrypted = _totpEncryption.Encrypt(plaintext);
            logs.Add($"Encrypted (prefix + first 20 chars): {encrypted[..Math.Min(24, encrypted.Length)]}…");
            logs.Add($"ENC: prefix present: {encrypted.StartsWith("ENC:")}");

            var decrypted = _totpEncryption.Decrypt(encrypted);
            var match = decrypted == plaintext;
            logs.Add($"Decrypted: {decrypted}");
            logs.Add($"Round-trip match: {match}");

            return Task.FromResult((match,
                match ? "AES-256-GCM encrypt/decrypt round-trip successful" : "Decryption mismatch — encryption key may differ",
                logs));
        }

        private async Task<(bool, string, List<string>)> TestJwtAsync()
        {
            var logs = new List<string>();
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "system-test@internal.local",
                Role = UserRole.SuperAdmin,
                IsActive = true,
                PasswordHash = "not-persisted",
                TotpSecret = "not-persisted",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            logs.Add($"Generating test JWT for: {testUser.Email} (role: {testUser.Role})");

            var token = _jwtService.CreateAccessToken(testUser);
            logs.Add($"Token generated ({token.Length} chars)");

            var jwtKey = await _secrets.GetSecretAsync(SecretKeys.JwtSigningKey);
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out var validatedToken);

            var jwt = (JwtSecurityToken)validatedToken;
            logs.Add($"Signature valid ✓");
            logs.Add($"Expires: {jwt.ValidTo:u}");
            logs.Add($"Claims:");
            foreach (var c in jwt.Claims) logs.Add($"  {c.Type} = {c.Value}");

            return (true, $"JWT signed and validated, expires {jwt.ValidTo:u}", logs);
        }

        // ── Connectivity ──────────────────────────────────────────────────────

        private async Task<(bool, string, List<string>)> TestGrpcPingAsync()
        {
            var logs = new List<string>();
            logs.Add("Sending probe to Notifications gRPC channel (3-second deadline)...");
            var (connected, detail) = await _grpcClient.PingAsync();
            logs.Add($"Connected: {connected}");
            logs.Add($"Detail:    {detail}");
            return (connected,
                connected ? $"gRPC channel ready — {detail}" : $"gRPC channel unavailable — {detail}",
                logs);
        }
    }
}
