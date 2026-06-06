using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Tickets.Commands.AddTicketComment;

public static class AddTicketComment
{
    public record Command(
        Guid TicketId,
        Guid UserId,
        string Content) : IRequest<Result<TicketCommentResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        }
    }

    public class Handler(IApplicationDbContext dbContext, IReferenceExtractor referenceExtractor)
        : IRequestHandler<Command, Result<TicketCommentResponse>>
    {
        public async Task<Result<TicketCommentResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var ticketExists = await dbContext.Tickets
                .AnyAsync(t => t.Id == request.TicketId, cancellationToken);

            if (!ticketExists)
            {
                return Result<TicketCommentResponse>.Failure("The ticket was not found.");
            }

            var comment = TicketComment.Create(
                request.TicketId,
                request.UserId,
                request.Content);

            dbContext.TicketComments.Add(comment);

            // NEX-24: Extract and persist entity references from comment content
            var references = referenceExtractor.Extract(request.Content);
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
                    comment.Id,
                    nameof(TicketComment),
                    reference.Type,
                    reference.Value,
                    targetId);

                dbContext.EntityReferences.Add(entityRef);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<TicketCommentResponse>.Success(new TicketCommentResponse(
                comment.Id,
                comment.TicketId,
                comment.UserId,
                comment.Content,
                comment.CreatedAt,
                comment.UpdatedAt));
        }
    }
}
