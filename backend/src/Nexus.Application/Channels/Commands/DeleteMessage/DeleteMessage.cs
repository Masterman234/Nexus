using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;

namespace Nexus.Application.Channels.Commands.DeleteMessage;

public static class DeleteMessage
{
    public record Command(Guid MessageId, Guid UserId) : IRequest<Result>;

    public class Handler(
        IApplicationDbContext dbContext,
        IChatService chatService)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var message = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

            if (message is null) return Result.Failure("Message not found.");

            // Authorization Check
            if (message.UserId != request.UserId)
            {
                return Result.Failure("Unauthorized to delete this message.");
            }

            var channelId = message.ChannelId;
            dbContext.Messages.Remove(message);
            await dbContext.SaveChangesAsync(cancellationToken);

            await chatService.BroadcastMessageDeletedAsync(channelId, request.MessageId, cancellationToken);

            return Result.Success();
        }
    }
}
