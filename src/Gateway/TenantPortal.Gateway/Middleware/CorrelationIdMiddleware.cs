using TenantPortal.Shared.Constants;

namespace TenantPortal.Gateway.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[AppConstants.Headers.CorrelationId].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.Request.Headers[AppConstants.Headers.CorrelationId] = correlationId;
            context.Response.Headers[AppConstants.Headers.CorrelationId] = correlationId;

            await _next(context);
        }
    }
}
