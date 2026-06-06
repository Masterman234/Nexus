using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;

namespace Nexus.Application.Channels.Queries.GetMessages;

public static class GetMessages
{
    public record Query(Guid ChannelId) : IRequest<Result<List<MessageResponse>>>;

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Query, Result<List<MessageResponse>>>
    {
        public async Task<Result<List<MessageResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var messages = await dbContext.Messages
                .Where(m => m.ChannelId == request.ChannelId)
                .OrderBy(m => m.SentAt)
                .Join(dbContext.Users,
                    m => m.UserId,
                    u => u.Id,
                    (m, u) => new { m, u })
                .Select(x => new MessageResponse(
                    x.m.Id,
                    x.m.Content,
                    x.u.Username,
                    x.m.ChannelId,
                    x.m.SentAt))
                .ToListAsync(cancellationToken);

            return Result<List<MessageResponse>>.Success(messages);
        }
    }
}
