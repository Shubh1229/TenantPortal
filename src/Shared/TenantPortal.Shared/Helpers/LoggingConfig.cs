using Serilog;
using Serilog.Events;

namespace TenantPortal.Shared.Helpers
{
    public static class LoggingConfig
    {
        private const string OutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Creates the standard Serilog configuration for all TenantPortal services.
        /// Writes to console and to a daily rolling file at /logs/{serviceName}/log-.txt,
        /// which is volume-mounted to ./logs/{serviceName} on the host for easy access outside Docker.
        /// </summary>
        public static LoggerConfiguration CreateDefault(string serviceName) =>
            new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .WriteTo.Console(outputTemplate: OutputTemplate)
                .WriteTo.File(
                    path: $"/logs/{serviceName}/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    outputTemplate: OutputTemplate);
    }
}
