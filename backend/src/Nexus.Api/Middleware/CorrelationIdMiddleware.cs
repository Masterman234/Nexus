using Microsoft.Extensions.Primitives;

namespace Nexus.Api.Middleware;

/// <summary>
/// Middleware to ensure every request has a Correlation ID for distributed tracing.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out StringValues correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

        // Add to logging context
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}