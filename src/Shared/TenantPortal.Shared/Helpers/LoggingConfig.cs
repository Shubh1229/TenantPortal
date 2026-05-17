using Serilog;
using Serilog.Events;

namespace TenantPortal.Shared.Helpers
{
    /// <summary>
    /// Centralised Serilog configuration for all TenantPortal services.
    ///
    /// Log files land at ./logs/{service}/{service}-YYYYMMDD.log on the host
    /// (volume-mounted from /logs inside the container). Each service gets its
    /// own folder and file so logs can be tailed independently.
    ///
    /// Level strategy:
    ///   Debug+   — all application code (services, controllers, middleware)
    ///   Info+    — EF Core SQL queries, YARP proxy forwarding, Serilog request log
    ///   Debug+   — ASP.NET Auth / Authorization (captures policy failures and JWT events)
    ///   Warning+ — everything else in Microsoft.* and System.* (framework noise)
    /// </summary>
    public static class LoggingConfig
    {
        private const string OutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        public static LoggerConfiguration CreateDefault(string serviceName) =>
            new LoggerConfiguration()
                // ── Application code ────────────────────────────────────────────────
                .MinimumLevel.Debug()

                // ── Suppress general framework noise ────────────────────────────────
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)

                // ── EF Core: show SQL queries at Information ─────────────────────────
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)

                // ── Auth pipeline: Debug so policy failures and JWT events are visible ─
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Debug)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authorization", LogEventLevel.Debug)

                // ── YARP: show proxy forwarding and upstream responses ───────────────
                .MinimumLevel.Override("Yarp.ReverseProxy", LogEventLevel.Information)

                // ── Sinks ────────────────────────────────────────────────────────────
                .WriteTo.Console(outputTemplate: OutputTemplate)
                .WriteTo.File(
                    path: $"/logs/{serviceName}/{serviceName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    outputTemplate: OutputTemplate);
    }
}
