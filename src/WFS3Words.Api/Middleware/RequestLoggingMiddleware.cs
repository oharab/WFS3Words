using System.Diagnostics;
using System.Text;

namespace WFS3Words.Api.Middleware;

/// <summary>
/// Middleware that logs detailed information about incoming HTTP requests,
/// including POST body content for debugging and monitoring purposes.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        // Log request start
        _logger.LogInformation(
            "[{RequestId}] {Method} {Path}{QueryString} - Started",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        // Log request headers if debug level enabled
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var headers = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"));
            _logger.LogDebug(
                "[{RequestId}] Headers: {Headers}",
                requestId,
                headers);
        }

        // Capture and log POST body
        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            context.Request.ContentLength > 0)
        {
            await LogRequestBodyAsync(context, requestId);
        }

        // Capture response status
        var originalBodyStream = context.Response.Body;
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "[{RequestId}] {Method} {Path}{QueryString} - Completed with {StatusCode} in {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task LogRequestBodyAsync(HttpContext context, string requestId)
    {
        context.Request.EnableBuffering();

        try
        {
            var body = await ReadRequestBodyAsync(context.Request);

            if (!string.IsNullOrWhiteSpace(body))
            {
                // Truncate very large bodies for logging
                var truncatedBody = body.Length > 4000
                    ? body[..4000] + "... (truncated)"
                    : body;

                _logger.LogInformation(
                    "[{RequestId}] POST Body ({ContentType}, {ContentLength} bytes): {Body}",
                    requestId,
                    context.Request.ContentType,
                    context.Request.ContentLength,
                    truncatedBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{RequestId}] Failed to read request body", requestId);
        }
        finally
        {
            // Reset the request body stream position so it can be read again
            context.Request.Body.Position = 0;
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(
            request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        return body;
    }
}
