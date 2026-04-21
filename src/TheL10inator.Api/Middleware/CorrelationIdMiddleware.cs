using Serilog.Context;

namespace TheL10inator.Api.Middleware;

/// <summary>
/// Reads the <c>X-Correlation-Id</c> request header (generating a new GUID when absent),
/// pushes it into Serilog's <see cref="LogContext"/> as <c>CorrelationId</c>, and echoes
/// the value back on the response header so clients can trace requests end-to-end.
/// Registered before <c>UseSerilogRequestLogging()</c> so the correlation id appears on
/// the request completion log entry.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
