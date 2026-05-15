using TenantPortal.Shared.Constants;

namespace TenantPortal.Gateway.Middleware
{
    /// <summary>
    /// Ensures every request flowing through the Gateway carries an <c>X-Correlation-ID</c> header.
    /// If the client provides one it is reused; otherwise a new UUID is generated.
    /// The ID is also added to the response so clients can correlate requests with server-side logs.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <inheritdoc/>
        public async Task InvokeAsync(HttpContext context)
        {
            // Preserve a caller-supplied ID so end-to-end tracing is possible from the client side
            var correlationId = context.Request.Headers[AppConstants.Headers.CorrelationId].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.Request.Headers[AppConstants.Headers.CorrelationId] = correlationId;
            context.Response.Headers[AppConstants.Headers.CorrelationId] = correlationId;

            await _next(context);
        }
    }
}
