using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;

namespace Nexus.Application.Workspaces.Queries.GetWorkspaces;

public static class GetWorkspaces
{
    public class Query : IRequest<Result<List<WorkspaceResponse>>>;

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Query, Result<List<WorkspaceResponse>>>
    {
        public async Task<Result<List<WorkspaceResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var workspaces = await dbContext.Workspaces
                .Select(w => new WorkspaceResponse(w.Id, w.Name, w.Description, w.CreatedAt))
                .ToListAsync(cancellationToken);

            return Result<List<WorkspaceResponse>>.Success(workspaces);
        }
    }
}
