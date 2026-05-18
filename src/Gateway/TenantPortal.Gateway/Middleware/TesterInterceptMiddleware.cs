using System.Security.Claims;
using TenantPortal.Shared.Constants;
using TenantPortal.Shared.Enums;

namespace TenantPortal.Gateway.Middleware
{
    /// <summary>
    /// Intercepts non-GET requests from Tester-role users before YARP forwards them downstream.
    /// Instead of persisting anything, it logs the attempted action and fires an email to the
    /// Super Admin via the Notifications service, then returns 200 to the frontend.
    /// The invite endpoint is an exception: it returns 403 (blocked, not simulated).
    /// </summary>
    public class TesterInterceptMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TesterInterceptMiddleware> _logger;

        private static readonly HashSet<string> _blockedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/invite"
        };

        public TesterInterceptMiddleware(
            RequestDelegate next,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TesterInterceptMiddleware> logger)
        {
            _next = next;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var role = context.User.FindFirstValue(AppConstants.Claims.UserRole);

            if (role != UserRole.Tester.ToString()
                || HttpMethods.IsGet(context.Request.Method)
                || HttpMethods.IsHead(context.Request.Method)
                || HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Invite is outright blocked — Testers must not be able to create accounts
            if (_blockedPaths.Contains(context.Request.Path.Value ?? string.Empty))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Tester accounts cannot send invites.");
                return;
            }

            // Buffer the body so we can read it without consuming the stream
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            var testerEmail = context.User.FindFirstValue(AppConstants.Claims.Email) ?? "unknown";
            var action = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";

            _logger.LogInformation(
                "TESTER intercept — user: {Email} | action: {Action} | body: {Body}",
                testerEmail, action, body);

            // Fire-and-forget — don't let email failure block the response
            _ = NotifyAsync(testerEmail, action, body);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{}");
        }

        private async Task NotifyAsync(string testerEmail, string action, string body)
        {
            try
            {
                var notificationsUrl = _configuration["Notifications:HttpUrl"] ?? "http://localhost:5004";
                var client = _httpClientFactory.CreateClient();
                await client.PostAsJsonAsync(
                    $"{notificationsUrl}/api/notifications/internal/tester-action",
                    new { TesterEmail = testerEmail, Action = action, Body = body });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send tester action notification email");
            }
        }
    }
}
