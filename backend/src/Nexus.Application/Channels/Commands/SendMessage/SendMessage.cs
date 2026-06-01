using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Channels.Commands.SendMessage;

public static class SendMessage
{
    public record Command(string Content, Guid UserId, Guid ChannelId) : IRequest<Result<MessageResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.ChannelId).NotEmpty();
        }
    }

    public class Handler(
        IApplicationDbContext dbContext,
        IChatService chatService)
        : IRequestHandler<Command, Result<MessageResponse>>
    {
        public async Task<Result<MessageResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user is null) return Result<MessageResponse>.Failure("User not found.");

            var message = Message.Create(request.Content, request.UserId, request.ChannelId);

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);

            var response = new MessageResponse(
                message.Id,
                message.Content,
                user.Username,
                message.ChannelId,
                message.SentAt);

            // Broadcast via abstraction to maintain Clean Architecture
            await chatService.BroadcastMessageAsync(request.ChannelId, response, cancellationToken);

            return Result<MessageResponse>.Success(response);
        }
    }
}
