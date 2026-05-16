using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using TenantPortal.Notifications.Data;
using TenantPortal.Notifications.Interfaces;
using TenantPortal.Notifications.Services;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Helpers;
using TenantPortal.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddOpenApi();

// Load the JWT signing key at startup so it matches the key used by the Auth service
var startupSecrets = new AzureVaultSecretsProvider("https://singhresidenthub-vault.vault.azure.net/"); // new LocalSecretsProvider();
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

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<ISecretsProvider>(
    new AzureVaultSecretsProvider("https://singhresidenthub-vault.vault.azure.net/"));

builder.Services.AddControllers();

// gRPC server — listens on the same port as REST via Http1AndHttp2 (configured in appsettings.json).
// Only internal services (Auth, Transactions) call these endpoints; they are not exposed to the gateway.
builder.Services.AddGrpc();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<TenantPortal.Notifications.Services.NotificationGrpcService>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}


app.Run();
