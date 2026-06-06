using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Application.ChatCommands;
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
        IChatService chatService,
        IChatCommandRouter commandRouter,
        IReferenceExtractor referenceExtractor)
        : IRequestHandler<Command, Result<MessageResponse>>
    {
        public async Task<Result<MessageResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user is null) return Result<MessageResponse>.Failure("User not found.");

            var message = Message.Create(request.Content, request.UserId, request.ChannelId);

            dbContext.Messages.Add(message);

            // NEX-24: Extract and persist entity references
            var references = referenceExtractor.Extract(request.Content);
            foreach (var reference in references)
            {
                Guid? targetId = null;
                // Basic resolution logic for tickets
                if (reference.Type == "Ticket" && int.TryParse(reference.Value.Replace("NEX-", "", StringComparison.OrdinalIgnoreCase), out var ticketNumber))
                {
                    targetId = await dbContext.Tickets
                        .Where(t => t.Number == ticketNumber)
                        .Select(t => t.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                }

                var entityRef = EntityReference.Create(
                    message.Id,
                    nameof(Message),
                    reference.Type,
                    reference.Value,
                    targetId);

                dbContext.EntityReferences.Add(entityRef);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var response = new MessageResponse(
                message.Id,
                message.Content,
                user.Username,
                message.ChannelId,
                message.SentAt);

            // Broadcast via abstraction to maintain Clean Architecture
            await chatService.BroadcastMessageAsync(request.ChannelId, response, cancellationToken);

            // NEX-18: if this message is a slash command (starts with `/`), hand it
            // to the router AFTER the user's message has already been persisted +
            // broadcast. The router runs the actual work in a fresh DI scope and
            // posts the bot reply via SignalR a few seconds later — the HTTP POST
            // here returns immediately. Same UX shape as Slack / Discord.
            var slashCommand = ChatCommand.TryParse(request.Content);
            if (slashCommand is not null)
            {
                commandRouter.Schedule(slashCommand, request.UserId, request.ChannelId);
            }

            return Result<MessageResponse>.Success(response);
        }
    }
}
