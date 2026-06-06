using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Application.Tickets.IntegrationEvents;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Tickets.Commands.TransitionTicketStatus;

public static class TransitionTicketStatus
{
    public record Command(
        Guid TicketId,
        TicketStatus NewStatus,
        Guid UserId) : IRequest<Result<TicketResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.NewStatus).IsInEnum();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public class Handler(IApplicationDbContext dbContext, IPublishEndpoint publishEndpoint)
        : IRequestHandler<Command, Result<TicketResponse>>
    {
        public async Task<Result<TicketResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var ticket = await dbContext.Tickets
                .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket is null)
            {
                return Result<TicketResponse>.Failure("The ticket was not found.");
            }

            if (ticket.Status == request.NewStatus)
            {
                return Result<TicketResponse>.Success(await MapToResponseAsync(ticket, cancellationToken));
            }

            var oldStatus = ticket.Status;
            ticket.ChangeStatus(request.NewStatus);

            var statusChange = TicketStatusChange.Create(
                ticket.Id,
                oldStatus,
                request.NewStatus,
                request.UserId);

            dbContext.TicketStatusChanges.Add(statusChange);
            await dbContext.SaveChangesAsync(cancellationToken);

            await publishEndpoint.Publish(new TicketStatusChangedIntegrationEvent(
                ticket.Id,
                oldStatus,
                request.NewStatus,
                request.UserId,
                ticket.UpdatedAt), cancellationToken);

            if (ticket.ResolvedAt.HasValue && oldStatus != TicketStatus.Done && oldStatus != TicketStatus.Closed)
            {
                await publishEndpoint.Publish(new TicketResolvedIntegrationEvent(
                    ticket.Id,
                    ticket.ResolvedAt.Value), cancellationToken);
            }

            return Result<TicketResponse>.Success(await MapToResponseAsync(ticket, cancellationToken));
        }

        private async Task<TicketResponse> MapToResponseAsync(Ticket ticket, CancellationToken ct)
        {
            var username = ticket.AssigneeUserId.HasValue
                ? await dbContext.Users
                    .Where(u => u.Id == ticket.AssigneeUserId.Value)
                    .Select(u => u.Username)
                    .FirstOrDefaultAsync(ct)
                : null;

            return new TicketResponse(
                ticket.Id,
                ticket.Number,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.Priority,
                ticket.AssigneeUserId,
                username,
                ticket.CreatorUserId,
                ticket.WorkspaceId,
                ticket.CreatedAt,
                ticket.UpdatedAt,
                ticket.ResolvedAt);
        }
    }
}
