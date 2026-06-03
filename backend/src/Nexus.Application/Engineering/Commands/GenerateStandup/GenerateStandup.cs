using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexus.Application.Abstractions;
using Nexus.Application.Engineering.Queries.UserActivity;
using Nexus.SharedKernel;
using System.Text;

namespace Nexus.Application.Engineering.Commands.GenerateStandup;

public static class GenerateStandup
{
    /// <summary>
    /// Generate a markdown standup summary.
    /// <para>
    /// • Pass <paramref name="UserId"/> for the canonical per-user projection
    ///   (NEX-16) — joins on real FKs where they exist.<br/>
    /// • Pass <paramref name="AuthorName"/> for the legacy LIKE-matching path
    ///   (kept so the existing <c>POST /api/v1/engineering/standup?authorName=</c>
    ///   query parameter still works while the frontend migrates to UserId).<br/>
    /// • Pass neither for a workspace-wide summary across all recent activity.
    /// </para>
    /// </summary>
    public record Command(Guid? UserId = null, string? AuthorName = null) : IRequest<Result<string>>;

    public class Handler(
        IApplicationDbContext dbContext,
        IUserActivityQuery userActivityQuery,
        IAIService aiService,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<string>>
    {
        // Lookback window — 48h covers "yesterday + this morning" across timezones
        // without bloating the LLM context with stale activity. Tunable later if
        // standups should default to "since last standup" or similar.
        private static readonly TimeSpan Lookback = TimeSpan.FromDays(2);

        // Keep the AI goal next to the handler that uses it. Once we have a second
        // command using the same instruction we can hoist this to a shared constant.
        private const string StandupGoal =
            "Generate a concise, professional daily standup summary. " +
            "Use bullet points for 'Completed' and 'In Progress'. " +
            "Highlight key impacts.";

        public async Task<Result<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            var until = DateTime.UtcNow;
            var since = until - Lookback;

            string contextBlock;

            if (request.UserId is Guid userId)
            {
                // Preferred path: real projection.
                logger.LogInformation(">>> [AI] Generating standup for UserId={UserId} window={Since}..{Until}",
                    userId, since, until);

                var activity = await userActivityQuery.GetAsync(userId, since, until, cancellationToken);

                logger.LogInformation(">>> [AI] Activity: {Commits} commits, {Prs} PRs, {Msgs} messages.",
                    activity.Commits.Count, activity.PullRequests.Count, activity.Messages.Count);

                if (activity.IsEmpty)
                {
                    logger.LogWarning(">>> [AI] No activity found for user in window.");
                    return Result<string>.Failure("No engineering activity found in the last 48 hours to summarize.");
                }

                contextBlock = FormatActivity(activity);
            }
            else
            {
                // Legacy path — name-LIKE matching, workspace-wide if AuthorName is null.
                // Kept until the frontend (Engineering Timeline standup button) passes a
                // real UserId. Will be deleted once NEX-18 lands the slash command, which
                // always has a UserId in context.
                contextBlock = await BuildLegacyContextAsync(request.AuthorName, since, cancellationToken);
                if (contextBlock.Length == 0)
                {
                    return Result<string>.Failure("No engineering activity found in the last 48 hours to summarize.");
                }
            }

            logger.LogInformation(">>> [AI] Calling Gemini service...");
            var result = await aiService.SummarizeActivityAsync(contextBlock, StandupGoal, cancellationToken);

            if (result.IsFailure)
            {
                logger.LogError(">>> [AI] Gemini service failed: {Error}", result.Error);
            }
            else
            {
                logger.LogInformation(">>> [AI] Standup summary generated successfully.");
            }

            return result;
        }

        private static string FormatActivity(Nexus.Application.Engineering.Queries.UserActivity.UserActivity a)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"USER: {a.DisplayName}");
            sb.AppendLine($"WINDOW: {a.Since:u} .. {a.Until:u}");

            sb.AppendLine("\nCOMMITS:");
            if (a.Commits.Count == 0) sb.AppendLine("- (none)");
            foreach (var c in a.Commits)
            {
                sb.AppendLine($"- [{c.RepositoryName}] {c.Message} (SHA: {c.Sha[..Math.Min(7, c.Sha.Length)]})");
            }

            sb.AppendLine("\nPULL REQUESTS:");
            if (a.PullRequests.Count == 0) sb.AppendLine("- (none)");
            foreach (var pr in a.PullRequests)
            {
                sb.AppendLine($"- [{pr.RepositoryName}] PR #{pr.Number}: {pr.Title} (State: {pr.State})");
            }

            sb.AppendLine("\nCHAT MESSAGES:");
            if (a.Messages.Count == 0) sb.AppendLine("- (none)");
            // Keep messages tight — the LLM does not need full chat history,
            // just enough to spot themes (questions, blockers, decisions).
            foreach (var line in a.Messages.Select(m => "- " + Snippet(m.Content)))
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        private static string Snippet(string content) =>
            content.Length > 160 ? content[..160] + "..." : content;

        private async Task<string> BuildLegacyContextAsync(string? authorName, DateTime since, CancellationToken ct)
        {
            logger.LogInformation(">>> [AI] (legacy path) Generating standup for author: {AuthorName}", authorName ?? "All");

            var commitsQuery = dbContext.Commits.Where(c => c.CommittedAt >= since);
            var prsQuery = dbContext.PullRequests.Where(pr => pr.UpdatedAt >= since);

            if (!string.IsNullOrWhiteSpace(authorName))
            {
                var author = authorName.ToLower();
                commitsQuery = commitsQuery.Where(c => c.AuthorName.ToLower().Contains(author));
                prsQuery = prsQuery.Where(pr => pr.AuthorName.ToLower().Contains(author));
            }

            var commits = await commitsQuery.OrderByDescending(c => c.CommittedAt).ToListAsync(ct);
            var prs = await prsQuery.OrderByDescending(pr => pr.UpdatedAt).ToListAsync(ct);

            logger.LogInformation(">>> [AI] (legacy path) Found {Commits} commits, {Prs} PRs.", commits.Count, prs.Count);

            if (commits.Count == 0 && prs.Count == 0)
            {
                logger.LogWarning(">>> [AI] (legacy path) No activity to summarize.");
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("COMMITS:");
            foreach (var c in commits)
            {
                sb.AppendLine($"- [{c.RepositoryName}] {c.Message} (Author: {c.AuthorName}, SHA: {c.Sha[..Math.Min(7, c.Sha.Length)]})");
            }
            sb.AppendLine("\nPULL REQUESTS:");
            foreach (var pr in prs)
            {
                sb.AppendLine($"- [{pr.RepositoryName}] PR #{pr.Number}: {pr.Title} (State: {pr.State}, Author: {pr.AuthorName})");
            }
            return sb.ToString();
        }
    }
}
