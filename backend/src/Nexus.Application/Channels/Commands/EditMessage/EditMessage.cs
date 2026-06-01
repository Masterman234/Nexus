using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Application.Channels;
using Nexus.SharedKernel;

namespace Nexus.Application.Channels.Commands.EditMessage;

public static class EditMessage
{
    public record Command(Guid MessageId, string Content, Guid UserId) : IRequest<Result<MessageResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.MessageId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public class Handler(
        IApplicationDbContext dbContext,
        IChatService chatService)
        : IRequestHandler<Command, Result<MessageResponse>>
    {
        public async Task<Result<MessageResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var message = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

            if (message is null) return Result<MessageResponse>.Failure("Message not found.");
            
            // Authorization Check
            if (message.UserId != request.UserId)
            {
                return Result<MessageResponse>.Failure("Unauthorized to edit this message.");
            }

            message.UpdateContent(request.Content);
            await dbContext.SaveChangesAsync(cancellationToken);

            var user = await dbContext.Users
                .FirstAsync(u => u.Id == message.UserId, cancellationToken);

            var response = new MessageResponse(
                message.Id,
                message.Content,
                user.Username,
                message.ChannelId,
                message.SentAt);

            await chatService.BroadcastMessageUpdatedAsync(message.ChannelId, response, cancellationToken);

            return Result<MessageResponse>.Success(response);
        }
    }
}
