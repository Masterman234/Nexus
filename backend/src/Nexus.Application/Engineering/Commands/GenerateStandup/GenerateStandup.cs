using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;
using System.Text;

namespace Nexus.Application.Engineering.Commands.GenerateStandup;

public static class GenerateStandup
{
    public record Command(string? AuthorName = null) : IRequest<Result<string>>;

    public class Handler(IApplicationDbContext dbContext, IAIService aiService, ILogger<Handler> logger) 
        : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation(">>> [AI] Generating standup summary for author: {AuthorName}", request.AuthorName ?? "All");
            
            // Expand to 48 hours to ensure recent activity is captured even across timezones
            var lookback = DateTime.UtcNow.AddDays(-2);

            // 1. Fetch recent commits
            var commitsQuery = dbContext.Commits
                .Where(c => c.CommittedAt >= lookback);

            if (!string.IsNullOrWhiteSpace(request.AuthorName))
            {
                var author = request.AuthorName.ToLower();
                commitsQuery = commitsQuery.Where(c => c.AuthorName.ToLower().Contains(author));
            }

            var commits = await commitsQuery
                .OrderByDescending(c => c.CommittedAt)
                .ToListAsync(cancellationToken);

            logger.LogInformation(">>> [AI] Found {Count} commits in lookback window.", commits.Count);

            // 2. Fetch recent PRs
            var prsQuery = dbContext.PullRequests
                .Where(pr => pr.UpdatedAt >= lookback);

            if (!string.IsNullOrWhiteSpace(request.AuthorName))
            {
                var author = request.AuthorName.ToLower();
                prsQuery = prsQuery.Where(pr => pr.AuthorName.ToLower().Contains(author));
            }

            var prs = await prsQuery
                .OrderByDescending(pr => pr.UpdatedAt)
                .ToListAsync(cancellationToken);

            logger.LogInformation(">>> [AI] Found {Count} PRs in lookback window.", prs.Count);

            if (commits.Count == 0 && prs.Count == 0)
            {
                logger.LogWarning(">>> [AI] No activity found to summarize.");
                return Result<string>.Failure("No engineering activity found in the last 48 hours to summarize.");
            }

            // 3. Format context for AI
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("COMMITS:");
            foreach (var c in commits)
            {
                contextBuilder.AppendLine($"- [{c.RepositoryName}] {c.Message} (Author: {c.AuthorName}, SHA: {c.Sha.Substring(0, 7)})");
            }

            contextBuilder.AppendLine("\nPULL REQUESTS:");
            foreach (var pr in prs)
            {
                contextBuilder.AppendLine($"- [{pr.RepositoryName}] PR #{pr.Number}: {pr.Title} (State: {pr.State}, Author: {pr.AuthorName})");
            }

            logger.LogInformation(">>> [AI] Calling Gemini service...");
            
            // 4. Call AI Service
            var goal = "Generate a concise, professional daily standup summary. Use bullet points for 'Completed' and 'In Progress'. Highlight key impacts.";
            
            var result = await aiService.SummarizeActivityAsync(contextBuilder.ToString(), goal, cancellationToken);
            
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
    }
}
