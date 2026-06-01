using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Nexus.Api.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip unnecessary paths
        if (IsStaticFile(context.Request.Path) ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        await LogRequest(context);

        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            await LogResponse(context, stopwatch.ElapsedMilliseconds);
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequest(HttpContext context)
    {
        context.Request.EnableBuffering();

        string requestBody = "[Empty]";

        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                leaveOpen: true);

            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        var headers = GetHeaders(context.Request.Headers);

        _logger.LogInformation(
            "HTTP Request: {Method} {Path}{Query} | Headers: {Headers} | Body: {Body}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            headers,
            SanitizeBody(requestBody));
    }

    private async Task LogResponse(HttpContext context, long elapsedMs)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        string responseBody = "[Empty]";

        // Only log JSON responses (avoid large files)
        if (context.Response.ContentType?.Contains("application/json") == true)
        {
            using var reader = new StreamReader(context.Response.Body);
            responseBody = await reader.ReadToEndAsync();
        }

        context.Response.Body.Seek(0, SeekOrigin.Begin);

        var headers = GetHeaders(context.Response.Headers);

        var logLevel = context.Response.StatusCode >= 400
            ? LogLevel.Warning
            : LogLevel.Information;

        _logger.Log(
            logLevel,
            "HTTP Response: {StatusCode} | Duration: {ElapsedMs}ms | Headers: {Headers} | Body: {Body}",
            context.Response.StatusCode,
            elapsedMs,
            headers,
            TruncateBody(responseBody));
    }

    private static string GetHeaders(IHeaderDictionary headers)
    {
        var sensitiveHeaders = new[] { "Authorization", "Cookie", "Set-Cookie" };
        var dict = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            dict[header.Key] = sensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase)
                ? "[REDACTED]"
                : header.Value.ToString();
        }

        return System.Text.Json.JsonSerializer.Serialize(dict);
    }

    private static string SanitizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "[Empty]";

        var sensitiveFields = new[] { "password", "token", "secret", "apikey" };

        foreach (var field in sensitiveFields)
        {
            body = System.Text.RegularExpressions.Regex.Replace(
                body,
                $@"(""{field}""\s*:\s*)""[^""]*""",
                $@"$1""[REDACTED]""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return TruncateBody(body);
    }

    private static string TruncateBody(string body, int maxLength = 2000)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "[Empty]";

        return body.Length <= maxLength
            ? body
            : body[..maxLength] + $"... [Truncated, total length: {body.Length}]";
    }

    private static bool IsStaticFile(PathString path)
    {
        var staticExtensions = new[]
        {
            ".css",".js",".png",".jpg",".jpeg",".gif",".svg",
            ".ico",".woff",".woff2",".ttf",".eot",".map"
        };

        return Array.Exists(staticExtensions, ext =>
            path.Value?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}

public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
