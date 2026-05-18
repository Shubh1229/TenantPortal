using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using TenantPortal.Auth.Data;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Services;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Helpers;
using TenantPortal.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = LoggingConfig.CreateDefault("auth").CreateLogger();

builder.Services.AddSerilog();

// Load the JWT signing key at startup so the key used to sign tokens matches
// the key used to validate them in all downstream services.
var startupSecrets = new AzureVaultSecretsProvider("https://singhresidenthub-vault.vault.azure.net/");   // new LocalSecretsProvider();
var jwtSigningKey = startupSecrets.GetSecretAsync(SecretKeys.JwtSigningKey).GetAwaiter().GetResult();
var totpEncryptionKey = startupSecrets.GetSecretAsync(SecretKeys.TotpEncryptionKey).GetAwaiter().GetResult();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppConstants.Policies.RequireSuperAdmin, policy =>
        policy.RequireClaim(AppConstants.Claims.UserRole, UserRole.SuperAdmin.ToString()));

    options.AddPolicy(AppConstants.Policies.RequireAdmin, policy =>
        policy.RequireClaim(AppConstants.Claims.UserRole,
            UserRole.Admin.ToString(),
            UserRole.SuperAdmin.ToString(),
            UserRole.Tester.ToString()));

    options.AddPolicy(AppConstants.Policies.RequireTenant, policy =>
        policy.RequireClaim(AppConstants.Claims.UserRole,
            UserRole.Tenant.ToString(),
            UserRole.Admin.ToString(),
            UserRole.SuperAdmin.ToString(),
            UserRole.Tester.ToString()));
});

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITotpService, TotpService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<SystemTestRunner>();
builder.Services.AddSingleton<ISecretsProvider>(
    new AzureVaultSecretsProvider("https://singhresidenthub-vault.vault.azure.net/"));

// Singleton: one gRPC channel shared across all requests (channels are thread-safe and expensive to create).
var notificationsGrpcUrl = builder.Configuration["Notifications:GrpcUrl"] ?? "http://localhost:8081";
builder.Services.AddSingleton<INotificationsGrpcClient>(sp =>
    new NotificationsGrpcClient(notificationsGrpcUrl, sp.GetRequiredService<ILogger<NotificationsGrpcClient>>()));

builder.Services.AddSingleton<ITotpEncryptionService>(
    new AesGcmTotpEncryptionService(totpEncryptionKey));

builder.Services.AddHttpClient();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var secrets = scope.ServiceProvider.GetRequiredService<ISecretsProvider>();
    var totpEnc = scope.ServiceProvider.GetRequiredService<ITotpEncryptionService>();
    await DbSeeder.SeedAsync(context, secrets, totpEnc);
}

app.Run();
