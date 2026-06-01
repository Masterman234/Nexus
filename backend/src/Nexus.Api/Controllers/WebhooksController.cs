using Asp.Versioning;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nexus.Application.Webhooks.Commands.HandleGithubWebhook;

namespace Nexus.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
// Route is versioned for consistency with the rest of the API. GitHub is configured
// with the RESOLVED URL (e.g. /api/v1/webhooks/github), not the template — so the
// {version:apiVersion} segment is harmless from GitHub's perspective. To evolve the
// payload contract later, ship a sibling controller with [ApiVersion("2.0")].
[Route("api/v{version:apiVersion}/webhooks")]
public class WebhooksController(ISender sender, IConfiguration configuration) : ControllerBase
{
    private const string SignatureHeader = "X-Hub-Signature-256";
    private const string EventHeader = "X-GitHub-Event";
    private const string DeliveryHeader = "X-GitHub-Delivery";

    /// <summary>
    /// Receives webhooks from GitHub. Verifies the HMAC-SHA256 signature against
    /// the configured Webhook:GithubSecret before accepting the payload.
    /// </summary>
    [HttpPost("github")]
    [Consumes("application/json")]
    public async Task<IActionResult> GithubWebhook(CancellationToken cancellationToken)
    {
        // Read the raw body. HMAC must be computed over the exact bytes GitHub signed,
        // so we cannot let the model binder reformat the JSON via [FromBody] JsonElement.
        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
            Request.Body.Position = 0;
        }

        var eventType = Request.Headers[EventHeader].ToString();
        var deliveryId = Request.Headers[DeliveryHeader].ToString();

        Console.WriteLine($">>> [API] Received Webhook! Event: {eventType} Delivery: {deliveryId}");

        var configuredSecret = configuration["Webhook:GithubSecret"];
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            var providedSignature = Request.Headers[SignatureHeader].ToString();
            if (string.IsNullOrWhiteSpace(providedSignature))
            {
                Console.WriteLine(">>> [API] WARN: signature header missing while secret is configured.");
                return Unauthorized(new { message = "Missing signature." });
            }

            if (!IsValidGithubSignature(rawBody, configuredSecret, providedSignature))
            {
                Console.WriteLine(">>> [API] WARN: signature mismatch.");
                return Unauthorized(new { message = "Invalid signature." });
            }
        }
        else
        {
            // No secret configured → treat as dev-mode. Loud log so this never sneaks into prod silently.
            Console.WriteLine(">>> [API] WARN: Webhook:GithubSecret is not configured. Skipping signature check (DEV ONLY).");
        }

        var actualEvent = string.IsNullOrWhiteSpace(eventType) ? "push" : eventType;

        var result = await sender.Send(new HandleGithubWebhook.Command(actualEvent, rawBody), cancellationToken);

        return result.IsSuccess ? Accepted() : BadRequest(new { message = result.Error });
    }

    private static bool IsValidGithubSignature(string payload, string secret, string providedHeader)
    {
        // GitHub sends "sha256=<hex>". Anything else is invalid.
        const string prefix = "sha256=";
        if (!providedHeader.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var providedHex = providedHeader[prefix.Length..];

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var computed = hmac.ComputeHash(payloadBytes);
        var computedHex = Convert.ToHexString(computed).ToLowerInvariant();

        // Constant-time comparison to avoid leaking timing info.
        var providedBytes = Encoding.ASCII.GetBytes(providedHex.ToLowerInvariant());
        var computedBytes = Encoding.ASCII.GetBytes(computedHex);
        return CryptographicOperations.FixedTimeEquals(providedBytes, computedBytes);
    }
}
