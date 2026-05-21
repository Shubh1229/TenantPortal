using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;
using TenantPortal.Shared.Helpers;
using TenantPortal.Shared.Interfaces;
using TenantPortal.Transactions.Data;
using TenantPortal.Transactions.Interfaces;
using TenantPortal.Transactions.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = LoggingConfig.CreateDefault("transactions").CreateLogger();

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

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddHttpClient();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IRentScheduleService, RentScheduleService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddSingleton<ISecretsProvider>(
    new AzureVaultSecretsProvider("https://singhresidenthub-vault.vault.azure.net/"));
builder.Services.AddHostedService<OverduePaymentJob>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    db.Database.Migrate();
}


app.Run();
