using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexus.Application.Abstractions;
using Nexus.Application.Channels;
using Nexus.Application.Engineering.IntegrationEvents;
using Nexus.Application.Webhooks.IntegrationEvents;
using Nexus.Domain.Entities;
using System.Text.Json;

namespace Nexus.Application.Webhooks.Consumers;

public class GithubWebhookConsumer(
    IApplicationDbContext dbContext,
    IChatService chatService,
    IConfiguration configuration,
    IReferenceExtractor referenceExtractor,
    ILogger<GithubWebhookConsumer> logger)
    : IConsumer<GithubWebhookReceivedIntegrationEvent>
{
    private const string DefaultPrMessage = "⚓ Pull Request activity on GitHub!";

    public async Task Consume(ConsumeContext<GithubWebhookReceivedIntegrationEvent> context)
    {
        var @event = context.Message;
        var ct = context.CancellationToken;

        logger.LogInformation(">>> [BOT] Consumer Start: {EventType}", @event.EventType);

        // NEX-15: Populate domain entities so the data is queryable, not just JSON.
        // NEX-10b: Collect integration events to publish AFTER entities are persisted,
        // so cross-context subscribers never see a "ghost" event for an entity that
        // failed to save. Publishing inside the same Consume() context lets MassTransit
        // route the messages through the same RabbitMQ connection without us touching DI.
        var integrationEvents = new List<object>();
        await ProcessEntitiesAsync(@event.EventType, @event.Payload, integrationEvents, ct);

        foreach (var evt in integrationEvents)
        {
            await context.Publish(evt, ct);
            logger.LogInformation(">>> [BOT] Published integration event: {EventType}", evt.GetType().Name);
        }

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

        logger.LogInformation(">>> [BOT] Broadcasting to UI channel={TargetChannelId} id={MessageId}", targetChannelId, message.Id);
        await chatService.BroadcastMessageAsync(targetChannelId.Value, new MessageResponse(
            message.Id,
            message.Content,
            SystemUsers.GithubBotUsername,
            targetChannelId.Value,
            message.SentAt), ct);
    }

    private async Task ProcessEntitiesAsync(string eventType, string payload, List<object> integrationEvents, CancellationToken ct)
    {
        try
        {
            logger.LogInformation(">>> [BOT] Parsing payload for entities. Event: {EventType}", eventType);
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (string.Equals(eventType, "push", StringComparison.OrdinalIgnoreCase))
            {
                await HandlePushEntitiesAsync(root, integrationEvents, ct);
            }
            else if (string.Equals(eventType, "pull_request", StringComparison.OrdinalIgnoreCase))
            {
                await HandlePullRequestEntitiesAsync(root, integrationEvents, ct);
            }
            else
            {
                logger.LogWarning(">>> [BOT] Unhandled event type for entities: {EventType}", eventType);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the whole consume — the raw payload is already in ExternalEvent
            // and the chat broadcast can still run. Integration events for *this* delivery
            // are forfeited but the audit log lets us replay if needed.
            integrationEvents.Clear();
            logger.LogError(ex, ">>> [BOT] Entity Process Error: {Message}", ex.Message);
        }
    }

    private async Task HandlePushEntitiesAsync(JsonElement root, List<object> integrationEvents, CancellationToken ct)
    {
        if (!root.TryGetProperty("repository", out var repoProp))
        {
            logger.LogWarning(">>> [BOT] No repository property found in push payload.");
            return;
        }

        string repoName = repoProp.GetProperty("full_name").GetString() ?? "unknown";
        logger.LogInformation(">>> [BOT] Processing push for repo: {RepoName}", repoName);

        if (root.TryGetProperty("commits", out var commitsProp) && commitsProp.ValueKind == JsonValueKind.Array)
        {
            // Stage newly-created Commit entities here so we can build integration events
            // from the same instances we just persisted (CommitId is the in-memory Guid).
            var newCommits = new List<Commit>();

            foreach (var commitElement in commitsProp.EnumerateArray())
            {
                string? sha = commitElement.GetProperty("id").GetString();
                if (sha is null)
                {
                    logger.LogWarning(">>> [BOT] Commit ID is null, skipping.");
                    continue;
                }

                if (await dbContext.Commits.AnyAsync(c => c.Sha == sha, ct))
                {
                    logger.LogInformation(">>> [BOT] Commit {Sha} already exists, skipping.", sha.Substring(0, 7));
                    continue;
                }

                var commit = Commit.Create(
                    sha,
                    commitElement.GetProperty("message").GetString() ?? "",
                    commitElement.GetProperty("author").GetProperty("name").GetString() ?? "Unknown",
                    commitElement.GetProperty("author").GetProperty("email").GetString() ?? "",
                    repoName,
                    commitElement.TryGetProperty("timestamp", out var ts) && ts.TryGetDateTime(out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow
                );

                dbContext.Commits.Add(commit);
                newCommits.Add(commit);

                // NEX-24: Extract and persist entity references from commit message
                var refs = referenceExtractor.Extract(commit.Message);
                foreach (var r in refs)
                {
                    Guid? targetId = null;
                    if (r.Type == "Ticket" && int.TryParse(r.Value.Replace("NEX-", "", StringComparison.OrdinalIgnoreCase), out var ticketNumber))
                    {
                        targetId = await dbContext.Tickets
                            .Where(t => t.Number == ticketNumber)
                            .Select(t => t.Id)
                            .FirstOrDefaultAsync(ct);
                    }

                    var entityRef = EntityReference.Create(
                        commit.Id,
                        nameof(Commit),
                        r.Type,
                        r.Value,
                        targetId);

                    dbContext.EntityReferences.Add(entityRef);
                }
            }

            if (newCommits.Count > 0)
            {
                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation(">>> [BOT] Saved {Count} new commits to DB.", newCommits.Count);

                // Only enqueue events after the SaveChanges round-trip succeeds — if the
                // DB write throws, we never publish a CommitPushedIntegrationEvent for
                // a commit that doesn't exist in our store.
                foreach (var c in newCommits)
                {
                    integrationEvents.Add(new CommitPushedIntegrationEvent(
                        c.Id, c.Sha, c.Message, c.AuthorName, c.AuthorEmail,
                        c.RepositoryName, c.CommittedAt));
                }
            }
            else
            {
                logger.LogInformation(">>> [BOT] No new commits to save.");
            }
        }
        else
        {
            logger.LogWarning(">>> [BOT] No commits array found in push payload.");
        }
    }

    private async Task HandlePullRequestEntitiesAsync(JsonElement root, List<object> integrationEvents, CancellationToken ct)
    {
        if (!root.TryGetProperty("pull_request", out var prElement))
        {
            logger.LogWarning(">>> [BOT] No pull_request property found in payload.");
            return;
        }

        long externalId = prElement.GetProperty("id").GetInt64();
        string repoName = root.TryGetProperty("repository", out var repoProp)
            ? repoProp.GetProperty("full_name").GetString() ?? "unknown"
            : "unknown";

        logger.LogInformation(">>> [BOT] Processing PR {ExternalId} for repo: {RepoName}", externalId, repoName);

        var existingPr = await dbContext.PullRequests.FirstOrDefaultAsync(pr => pr.ExternalId == externalId, ct);

        string title = prElement.GetProperty("title").GetString() ?? "";
        string description = prElement.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";
        string state = prElement.GetProperty("state").GetString() ?? "open";
        string url = prElement.GetProperty("html_url").GetString() ?? "";
        string authorName = prElement.GetProperty("user").GetProperty("login").GetString() ?? "Unknown";
        int number = prElement.GetProperty("number").GetInt32();

        DateTime createdAt = prElement.GetProperty("created_at").GetDateTime().ToUniversalTime();
        DateTime updatedAt = prElement.GetProperty("updated_at").GetDateTime().ToUniversalTime();
        DateTime? mergedAt = prElement.TryGetProperty("merged_at", out var ma) && ma.ValueKind != JsonValueKind.Null && ma.TryGetDateTime(out var mdt)
            ? mdt.ToUniversalTime()
            : null;

        // Capture "was this PR merged on this delivery?" BEFORE we mutate the entity,
        // so a webhook landing exactly at merge time emits PullRequestMerged once.
        bool wasMerged = existingPr?.MergedAt is not null;
        bool isNowMerged = mergedAt is not null;
        bool isNewlyOpened = existingPr is null;

        PullRequest pr;
        if (existingPr is null)
        {
            logger.LogInformation(">>> [BOT] Creating new PR entity: {Title}", title);
            pr = PullRequest.Create(
                externalId, number, title, description, state, url,
                repoName, authorName, createdAt, updatedAt, mergedAt);

            dbContext.PullRequests.Add(pr);
        }
        else
        {
            logger.LogInformation(">>> [BOT] Updating existing PR entity: {Title}", title);
            existingPr.Update(title, description, state, updatedAt, mergedAt);
            pr = existingPr;
        }

        // NEX-24: Extract and persist entity references from PR title and description
        var combinedText = $"{title} {description}";
        var prRefs = referenceExtractor.Extract(combinedText);

        // Remove existing references for this PR to avoid duplicates on update
        if (existingPr is not null)
        {
            var oldRefs = await dbContext.EntityReferences
                .Where(er => er.SourceEntityId == pr.Id && er.SourceEntityType == nameof(PullRequest))
                .ToListAsync(ct);

            foreach (var old in oldRefs) dbContext.EntityReferences.Remove(old);
        }

        foreach (var r in prRefs)
        {
            Guid? targetId = null;
            if (r.Type == "Ticket" && int.TryParse(r.Value.Replace("NEX-", "", StringComparison.OrdinalIgnoreCase), out var ticketNumber))
            {
                targetId = await dbContext.Tickets
                    .Where(t => t.Number == ticketNumber)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync(ct);
            }

            var entityRef = EntityReference.Create(
                pr.Id,
                nameof(PullRequest),
                r.Type,
                r.Value,
                targetId);

            dbContext.EntityReferences.Add(entityRef);
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation(">>> [BOT] PR saved to DB.");

        // Publish integration events AFTER save. Two distinct signals:
        //  - "opened" fires once when a brand-new PR row is created.
        //  - "merged" fires on the delivery where the PR transitions to having
        //    a merged_at timestamp. Idempotent: a webhook redelivery against an
        //    already-merged PR (wasMerged == true) will NOT re-emit.
        if (isNewlyOpened)
        {
            integrationEvents.Add(new PullRequestOpenedIntegrationEvent(
                pr.Id, pr.ExternalId, pr.Number, pr.Title, pr.Description,
                pr.Url, pr.RepositoryName, pr.AuthorName, pr.CreatedAt));
        }

        if (isNowMerged && !wasMerged)
        {
            integrationEvents.Add(new PullRequestMergedIntegrationEvent(
                pr.Id, pr.ExternalId, pr.Number, pr.Title, pr.Url,
                pr.RepositoryName, pr.AuthorName, pr.MergedAt!.Value));
        }
    }

    private async Task<Guid?> ResolveTargetChannelIdAsync(CancellationToken ct)
    {
        var configured = configuration["Webhook:GithubTargetChannelId"];
        if (Guid.TryParse(configured, out var parsed))
        {
            var exists = await dbContext.Channels.AnyAsync(c => c.Id == parsed, ct);
            if (!exists)
            {
                logger.LogError(">>> [BOT] ERROR: Webhook:GithubTargetChannelId is set to {Parsed} but no channel with that id exists.", parsed);
                return null;
            }
            return parsed;
        }

        // Legacy fallback for first-run dev: pick the OLDEST channel named 'general'.
        // Logging this loudly so misconfig in prod doesn't go unnoticed.
        logger.LogWarning(">>> [BOT] WARN: Webhook:GithubTargetChannelId not configured. Falling back to oldest 'general' channel.");

        var fallback = await dbContext.Channels
            .Where(c => c.Name == "general")
            .OrderBy(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Name })
            .FirstOrDefaultAsync(ct);

        if (fallback is null)
        {
            logger.LogError(">>> [BOT] ERROR: No channel named 'general' found in DB.");
            return null;
        }

        logger.LogInformation(">>> [BOT] Bot will post to channel '{ChannelName}' (Id: {ChannelId}). Ensure your UI is viewing this channel.", fallback.Name, fallback.Id);
        return fallback.Id;
    }

    private string BuildDisplayMessage(string eventType, string payload)
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

    private string ParsePushEvent(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return "🚀 New code pushed to GitHub!";

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string repoName = GetPropertyValue(root, "repository", "full_name") ?? "Nexus-Platform";
            string pusherName = GetPropertyValue(root, "pusher", "name") ?? "A Developer";

            var commitMsgs = new List<string>();
            if (root.TryGetProperty("commits", out var commitProp) && commitProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in commitProp.EnumerateArray())
                {
                    var msg = c.TryGetProperty("message", out var m) ? m.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        // Take first line and truncate if very long
                        var firstLine = msg.Split('\n')[0];
                        if (firstLine.Length > 80) firstLine = firstLine.Substring(0, 77) + "...";
                        commitMsgs.Add($"> `{firstLine}`");
                    }
                }
            }

            var summary = $"🚀 **{pusherName}** just pushed {commitMsgs.Count} commit(s) to `{repoName}`";

            if (commitMsgs.Count > 0)
            {
                // Only show first 3 commits to avoid spamming
                var displayCommits = commitMsgs.Take(3).ToList();
                var list = string.Join("\n", displayCommits);
                if (commitMsgs.Count > 3) list += $"\n> ... and {commitMsgs.Count - 3} more";

                return $"{summary}\n\n{list}";
            }

            return summary;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, ">>> [BOT] Parse Warning in ParsePushEvent: {Message}", ex.Message);
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
