using MassTransit;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Application.Channels;
using Nexus.Application.Webhooks.IntegrationEvents;
using Nexus.Domain.Entities;
using System.Text.Json;

namespace Nexus.Application.Webhooks.Consumers;

public class GithubWebhookConsumer(
    IApplicationDbContext dbContext,
    IChatService chatService)
    : IConsumer<GithubWebhookReceivedIntegrationEvent>
{
    private const string DefaultPrMessage = "⚓ Pull Request activity on GitHub!";

    public async Task Consume(ConsumeContext<GithubWebhookReceivedIntegrationEvent> context)
    {
        var @event = context.Message;
        var ct = context.CancellationToken;

        Console.WriteLine($">>> [BOT] Consumer Start: {@event.EventType}");

        var displayMessage = BuildDisplayMessage(@event.EventType, @event.Payload);

        // Case-insensitive search for the 'general' channel.
        var allChannels = await dbContext.Channels.ToListAsync(ct);
        var targetChannelIds = allChannels
            .Where(c => string.Equals(c.Name, "general", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Id)
            .ToList();

        if (targetChannelIds.Count == 0)
        {
            Console.WriteLine(">>> [BOT] ERROR: No channel named 'general' found in DB!");
            return;
        }

        // Persist each broadcast as a real Message row so history survives a reload
        // and clients that weren't joined to the channel at delivery time still see
        // it when they later open the channel. The github-bot system user is seeded
        // by DatabaseInitializer; its FK is guaranteed to exist.
        foreach (var channelId in targetChannelIds)
        {
            var message = Message.Create(displayMessage, SystemUsers.GithubBotId, channelId);
            dbContext.Messages.Add(message);
        }

        await dbContext.SaveChangesAsync(ct);

        // Broadcast after persistence so the on-wire payload matches the DB row.
        foreach (var channelId in targetChannelIds)
        {
            // Find the just-added message for this channel. We re-pull from the
            // tracker rather than threading the entity out of the loop above so
            // the broadcast id matches the persisted id exactly.
            var persisted = await dbContext.Messages
                .Where(m => m.ChannelId == channelId && m.UserId == SystemUsers.GithubBotId)
                .OrderByDescending(m => m.SentAt)
                .FirstAsync(ct);

            Console.WriteLine($">>> [BOT] Broadcasting to UI channel={channelId} id={persisted.Id}");
            await chatService.BroadcastMessageAsync(channelId, new MessageResponse(
                persisted.Id,
                persisted.Content,
                SystemUsers.GithubBotUsername,
                channelId,
                persisted.SentAt), ct);
        }
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
