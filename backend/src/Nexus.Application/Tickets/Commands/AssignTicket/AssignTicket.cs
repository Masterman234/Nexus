using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Application.Tickets.IntegrationEvents;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Tickets.Commands.AssignTicket;

public static class AssignTicket
{
    public record Command(
        Guid TicketId,
        Guid? AssigneeUserId) : IRequest<Result<TicketResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
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

            ticket.Assign(request.AssigneeUserId);

            await dbContext.SaveChangesAsync(cancellationToken);

            await publishEndpoint.Publish(new TicketAssignedIntegrationEvent(
                ticket.Id,
                ticket.AssigneeUserId,
                ticket.UpdatedAt), cancellationToken);

            var username = request.AssigneeUserId.HasValue
                ? await dbContext.Users
                    .Where(u => u.Id == request.AssigneeUserId.Value)
                    .Select(u => u.Username)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            return Result<TicketResponse>.Success(new TicketResponse(
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
                ticket.ResolvedAt));
        }
    }
}
