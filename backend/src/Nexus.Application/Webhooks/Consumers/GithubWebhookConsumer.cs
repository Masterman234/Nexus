using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nexus.Application.Abstractions;
using Nexus.Application.Channels;
using Nexus.Application.Webhooks.IntegrationEvents;
using Nexus.Domain.Entities;
using System.Text.Json;

namespace Nexus.Application.Webhooks.Consumers;

public class GithubWebhookConsumer(
    IApplicationDbContext dbContext,
    IChatService chatService,
    IConfiguration configuration)
    : IConsumer<GithubWebhookReceivedIntegrationEvent>
{
    private const string DefaultPrMessage = "⚓ Pull Request activity on GitHub!";

    public async Task Consume(ConsumeContext<GithubWebhookReceivedIntegrationEvent> context)
    {
        var @event = context.Message;
        var ct = context.CancellationToken;

        Console.WriteLine($">>> [BOT] Consumer Start: {@event.EventType}");

        var displayMessage = BuildDisplayMessage(@event.EventType, @event.Payload);

        // Resolve target channel from config so we don't accidentally fan out to every
        // workspace's auto-created "general" channel. When unset (early dev), fall back
        // to the legacy name-match behaviour and log a warning so misconfig is loud.
        var targetChannelId = await ResolveTargetChannelIdAsync(ct);
        if (targetChannelId is null)
        {
            return; // ResolveTargetChannelIdAsync already logged the reason.
        }

        // Persist as a real Message row so history survives a reload and clients that
        // weren't joined to the channel at delivery time still see it on next open.
        // The github-bot system user is seeded by DatabaseInitializer; FK is safe.
        var message = Message.Create(displayMessage, SystemUsers.GithubBotId, targetChannelId.Value);
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(ct);

        Console.WriteLine($">>> [BOT] Broadcasting to UI channel={targetChannelId} id={message.Id}");
        await chatService.BroadcastMessageAsync(targetChannelId.Value, new MessageResponse(
            message.Id,
            message.Content,
            SystemUsers.GithubBotUsername,
            targetChannelId.Value,
            message.SentAt), ct);
    }

    private async Task<Guid?> ResolveTargetChannelIdAsync(CancellationToken ct)
    {
        var configured = configuration["Webhook:GithubTargetChannelId"];
        if (Guid.TryParse(configured, out var parsed))
        {
            var exists = await dbContext.Channels.AnyAsync(c => c.Id == parsed, ct);
            if (!exists)
            {
                Console.WriteLine($">>> [BOT] ERROR: Webhook:GithubTargetChannelId is set to {parsed} but no channel with that id exists.");
                return null;
            }
            return parsed;
        }

        // Legacy fallback for first-run dev: pick the OLDEST channel named 'general'.
        // Logging this loudly so misconfig in prod doesn't go unnoticed.
        Console.WriteLine(">>> [BOT] WARN: Webhook:GithubTargetChannelId not configured. Falling back to oldest channel named 'general'.");
        var fallback = await dbContext.Channels
            .Where(c => c.Name.ToLower() == "general")
            .OrderBy(c => c.CreatedAt)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);

        if (fallback is null)
        {
            Console.WriteLine(">>> [BOT] ERROR: No channel named 'general' found in DB.");
        }
        return fallback;
    }

    private static string BuildDisplayMessage(string eventType, string payload)
    {
        if (string.Equals(eventType, "push", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePushEvent(payload);
        }
        if (string.Equals(eventType, "pull_request", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultPrMessage;
        }
        return $"⚡ GitHub Event Received: {eventType}";
    }

    private static string ParsePushEvent(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return "🚀 New code pushed to GitHub!";

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string repoName = GetPropertyValue(root, "repository", "full_name") ?? "Nexus-Platform";
            string pusherName = GetPropertyValue(root, "pusher", "name") ?? "A Developer";

            int commits = 0;
            if (root.TryGetProperty("commits", out var commitProp) && commitProp.ValueKind == JsonValueKind.Array)
            {
                commits = commitProp.GetArrayLength();
            }

            return $"🚀 **{pusherName}** just pushed {commits} commit(s) to `{repoName}`";
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> [BOT] Parse Warning: {ex.Message}");
            return "🚀 New code pushed to GitHub!";
        }
    }

    private static string? GetPropertyValue(JsonElement root, string parent, string child)
    {
        if (!root.TryGetProperty(parent, out var parentProp)) return null;

        if (parentProp.ValueKind == JsonValueKind.Object)
        {
            return parentProp.TryGetProperty(child, out var childProp) ? childProp.GetString() : null;
        }

        return parentProp.GetString();
    }
}
