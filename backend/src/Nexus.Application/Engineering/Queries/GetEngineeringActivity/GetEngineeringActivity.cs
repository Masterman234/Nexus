using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;

namespace Nexus.Application.Engineering.Queries.GetEngineeringActivity;

public static class GetEngineeringActivity
{
    public class Query : IRequest<Result<EngineeringActivityResponse>>;

    public class Handler(IApplicationDbContext dbContext) 
        : IRequestHandler<Query, Result<EngineeringActivityResponse>>
    {
        public async Task<Result<EngineeringActivityResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var commits = await dbContext.Commits
                .OrderByDescending(c => c.CommittedAt)
                .Take(50)
                .Select(c => new CommitResponse(
                    c.Id,
                    c.Sha,
                    c.Message,
                    c.AuthorName,
                    c.AuthorEmail,
                    c.RepositoryName,
                    c.CommittedAt))
                .ToListAsync(cancellationToken);

            var pullRequests = await dbContext.PullRequests
                .OrderByDescending(pr => pr.UpdatedAt)
                .Take(50)
                .Select(pr => new PullRequestResponse(
                    pr.Id,
                    pr.ExternalId,
                    pr.Number,
                    pr.Title,
                    pr.Description,
                    pr.State,
                    pr.Url,
                    pr.RepositoryName,
                    pr.AuthorName,
                    pr.CreatedAt,
                    pr.UpdatedAt,
                    pr.MergedAt))
                .ToListAsync(cancellationToken);

            return Result<EngineeringActivityResponse>.Success(
                new EngineeringActivityResponse(commits, pullRequests));
        }
    }
}
