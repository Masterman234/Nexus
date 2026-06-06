using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Tickets.Commands.UpdateTicket;

public static class UpdateTicket
{
    public record Command(
        Guid TicketId,
        string Title,
        string Description,
        TicketPriority Priority) : IRequest<Result<TicketResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Priority).IsInEnum();
        }
    }

    public class Handler(IApplicationDbContext dbContext, IReferenceExtractor referenceExtractor)
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

            ticket.Update(request.Title, request.Description, request.Priority);

            // NEX-24: Update entity references
            var combinedText = $"{request.Title} {request.Description}";
            var references = referenceExtractor.Extract(combinedText);

            // Remove existing references for this Ticket to avoid duplicates on update
            var oldRefs = await dbContext.EntityReferences
                .Where(er => er.SourceEntityId == ticket.Id && er.SourceEntityType == nameof(Ticket))
                .ToListAsync(cancellationToken);

            foreach (var old in oldRefs) dbContext.EntityReferences.Remove(old);

            foreach (var reference in references)
            {
                Guid? targetId = null;
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

            var username = ticket.AssigneeUserId.HasValue
                ? await dbContext.Users
                    .Where(u => u.Id == ticket.AssigneeUserId.Value)
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
