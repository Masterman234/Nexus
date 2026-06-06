using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Engineering.Queries.ListIncidents;

public record IncidentListItemResponse(
    Guid Id,
    string Title,
    IncidentStatus Status,
    IncidentSeverity Severity,
    Guid DeclaredByUserId,
    Guid? DedicatedChannelId,
    DateTime CreatedAt);

public static class ListIncidents
{
    public record Query(Guid WorkspaceId) : IRequest<Result<List<IncidentListItemResponse>>>;

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Query, Result<List<IncidentListItemResponse>>>
    {
        public async Task<Result<List<IncidentListItemResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var incidents = await dbContext.Incidents
                .Where(i => i.WorkspaceId == request.WorkspaceId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new IncidentListItemResponse(
                    i.Id,
                    i.Title,
                    i.Status,
                    i.Severity,
                    i.DeclaredByUserId,
                    i.DedicatedChannelId,
                    i.CreatedAt))
                .ToListAsync(cancellationToken);

            return Result<List<IncidentListItemResponse>>.Success(incidents);
        }
    }
}
