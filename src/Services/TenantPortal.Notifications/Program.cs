using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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

// Port 8080: HTTP/1.1 for REST (gateway-facing).
// Port 8081: HTTP/2 cleartext (h2c) for gRPC — TLS is terminated at the ingress/load-balancer.
// Http1AndHttp2 on a single port requires TLS for HTTP/2 negotiation (ALPN), so we use two listeners.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http1);
    serverOptions.ListenAnyIP(8081, o => o.Protocols = HttpProtocols.Http2);
});

Log.Logger = LoggingConfig.CreateDefault("notifications").CreateLogger();

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

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<ISecretsProvider>(
    new AzureVaultSecretsProvider("https://singhresidenthub-vault.vault.azure.net/"));

builder.Services.AddControllers();

// gRPC server on port 8081 (HTTP/2 only). REST stays on 8080 (HTTP/1.1 only).
builder.Services.AddGrpc();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
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
