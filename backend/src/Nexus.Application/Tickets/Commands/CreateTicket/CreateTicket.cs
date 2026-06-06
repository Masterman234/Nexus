using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Application.Tickets.IntegrationEvents;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Tickets.Commands.CreateTicket;

public static class CreateTicket
{
    public record Command(
        string Title,
        string Description,
        TicketPriority Priority,
        Guid CreatorUserId,
        Guid WorkspaceId) : IRequest<Result<TicketResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Priority).IsInEnum();
            RuleFor(x => x.CreatorUserId).NotEmpty();
            RuleFor(x => x.WorkspaceId).NotEmpty();
        }
    }

    public class Handler(
        IApplicationDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        IReferenceExtractor referenceExtractor)
        : IRequestHandler<Command, Result<TicketResponse>>
    {
        public async Task<Result<TicketResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Calculate next ticket number for the workspace.
            // In a production environment, this should be handled by a more robust
            // sequence or distributed lock to avoid collisions.
            var nextNumber = (await dbContext.Tickets
                .Where(t => t.WorkspaceId == request.WorkspaceId)
                .MaxAsync(t => (int?)t.Number, cancellationToken) ?? 0) + 1;

            var ticket = Ticket.Create(
                nextNumber,
                request.Title,
                request.Description,
                TicketStatus.Open,
                request.Priority,
                request.CreatorUserId,
                request.WorkspaceId);

            // Create initial audit record
            var statusChange = TicketStatusChange.Create(
                ticket.Id,
                null, // Old status is null for creation
                TicketStatus.Open,
                request.CreatorUserId);

            dbContext.Tickets.Add(ticket);
            dbContext.TicketStatusChanges.Add(statusChange);

            // NEX-24: Extract and persist entity references from ticket description
            var combinedText = $"{request.Title} {request.Description}";
            var references = referenceExtractor.Extract(combinedText);
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
                    ticket.Id,
                    nameof(Ticket),
                    reference.Type,
                    reference.Value,
                    targetId);

                dbContext.EntityReferences.Add(entityRef);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await publishEndpoint.Publish(new TicketCreatedIntegrationEvent(
                ticket.Id,
                ticket.Number,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.Priority,
                ticket.CreatorUserId,
                ticket.WorkspaceId,
                ticket.CreatedAt), cancellationToken);

            return Result<TicketResponse>.Success(new TicketResponse(
                ticket.Id,
                ticket.Number,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.Priority,
                ticket.AssigneeUserId,
                null, // AssigneeUsername is null on creation
                ticket.CreatorUserId,
                ticket.WorkspaceId,
                ticket.CreatedAt,
                ticket.UpdatedAt,
                ticket.ResolvedAt));
        }
    }
}
