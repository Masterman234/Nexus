using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;
using System.Text;

namespace Nexus.Application.Engineering.Commands.GenerateStandup;

public static class GenerateStandup
{
    public record Command(string? AuthorName = null) : IRequest<Result<string>>;

    public class Handler(IApplicationDbContext dbContext, IAIService aiService) 
        : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            var yesterday = DateTime.UtcNow.AddDays(-1);

            // 1. Fetch recent commits
            var commitsQuery = dbContext.Commits
                .Where(c => c.CommittedAt >= yesterday);

            if (!string.IsNullOrWhiteSpace(request.AuthorName))
            {
                var author = request.AuthorName.ToLower();
                commitsQuery = commitsQuery.Where(c => c.AuthorName.ToLower().Contains(author));
            }

            var commits = await commitsQuery
                .OrderByDescending(c => c.CommittedAt)
                .ToListAsync(cancellationToken);

            // 2. Fetch recent PRs
            var prsQuery = dbContext.PullRequests
                .Where(pr => pr.UpdatedAt >= yesterday);

            if (!string.IsNullOrWhiteSpace(request.AuthorName))
            {
                var author = request.AuthorName.ToLower();
                prsQuery = prsQuery.Where(pr => pr.AuthorName.ToLower().Contains(author));
            }

            var prs = await prsQuery
                .OrderByDescending(pr => pr.UpdatedAt)
                .ToListAsync(cancellationToken);

            if (commits.Count == 0 && prs.Count == 0)
            {
                return Result<string>.Failure("No engineering activity found in the last 24 hours to summarize.");
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

            // 4. Call AI Service
            var goal = "Generate a concise, professional daily standup summary. Use bullet points for 'Completed' and 'In Progress'. Highlight key impacts.";
            
            return await aiService.SummarizeActivityAsync(contextBuilder.ToString(), goal, cancellationToken);
        }
    }
}
