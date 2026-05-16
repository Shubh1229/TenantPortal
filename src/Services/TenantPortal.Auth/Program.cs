using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using TenantPortal.Auth.Data;
using TenantPortal.Auth.Interfaces;
using TenantPortal.Auth.Services;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Helpers;
using TenantPortal.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

// Load the JWT signing key at startup so the key used to sign tokens matches
// the key used to validate them in all downstream services.
var startupSecrets = new AzureVaultSecretsProvider("https://singhrentalhub-vault.vault.azure.net/");   // new LocalSecretsProvider();
var jwtSigningKey = startupSecrets.GetSecretAsync(SecretKeys.JwtSigningKey).GetAwaiter().GetResult();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
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
            UserRole.SuperAdmin.ToString()));

    options.AddPolicy(AppConstants.Policies.RequireTenant, policy =>
        policy.RequireClaim(AppConstants.Claims.UserRole,
            UserRole.Tenant.ToString(),
            UserRole.Admin.ToString(),
            UserRole.SuperAdmin.ToString()));
});

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITotpService, TotpService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISecretsProvider, LocalSecretsProvider>();

// Singleton: one gRPC channel shared across all requests (channels are thread-safe and expensive to create).
var notificationsGrpcUrl = builder.Configuration["Notifications:GrpcUrl"] ?? "http://localhost:5004";
builder.Services.AddSingleton<INotificationsGrpcClient>(
    new NotificationsGrpcClient(notificationsGrpcUrl));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
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
    await DbSeeder.SeedAsync(context);
}

app.Run();
