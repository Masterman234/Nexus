using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Channels.Queries.GetChannels;

public static class GetChannels
{
    public record Query(Guid? WorkspaceId = null) : IRequest<Result<List<ChannelResponse>>>;

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Query, Result<List<ChannelResponse>>>
    {
        public async Task<Result<List<ChannelResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            // CLEAN SEEDING: Use robust string comparison for the analyzer
            var channelsExist = await dbContext.Channels
                .AnyAsync(c => EF.Functions.Like(c.Name, "general"), cancellationToken);

            if (!channelsExist)
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(cancellationToken);
                var ownerId = user?.Id ?? Guid.NewGuid();

                var defaultWorkspace = await dbContext.Workspaces.FirstOrDefaultAsync(cancellationToken);
                if (defaultWorkspace == null)
                {
                    defaultWorkspace = Workspace.Create("Main Workspace", "Auto-generated", ownerId);
                    dbContext.Workspaces.Add(defaultWorkspace);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var defaultChannel = Channel.Create("general", "General discussion", defaultWorkspace.Id);
                dbContext.Channels.Add(defaultChannel);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var query = dbContext.Channels.AsQueryable();

            if (request.WorkspaceId.HasValue && request.WorkspaceId.Value != Guid.Empty)
            {
                query = query.Where(c => c.WorkspaceId == request.WorkspaceId.Value);
            }

            var channels = await query
                .Select(c => new ChannelResponse(c.Id, c.Name, c.Description, c.WorkspaceId, c.CreatedAt))
                .ToListAsync(cancellationToken);

            if (channels.Count == 0)
            {
                 channels = await dbContext.Channels
                    .Select(c => new ChannelResponse(c.Id, c.Name, c.Description, c.WorkspaceId, c.CreatedAt))
                    .ToListAsync(cancellationToken);
            }

            return Result<List<ChannelResponse>>.Success(channels);
        }
    }
}
