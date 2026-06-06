using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;
using Nexus.Application.Tickets.Queries.GetTicketByNumber;

namespace Nexus.Application.Tickets.Queries.GetTicketById;

public static class GetTicketById
{
    public record Query(Guid TicketId) : IRequest<Result<TicketDetailResponse>>;

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Query, Result<TicketDetailResponse>>
    {
        public async Task<Result<TicketDetailResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var ticketResponse = await (from t in dbContext.Tickets
                                        join u in dbContext.Users on t.AssigneeUserId equals u.Id into users
                                        from u in users.DefaultIfEmpty()
                                        where t.Id == request.TicketId
                                        select new TicketResponse(
                                            t.Id,
                                            t.Number,
                                            t.Title,
                                            t.Description,
                                            t.Status,
                                            t.Priority,
                                            t.AssigneeUserId,
                                            u != null ? u.Username : null,
                                            t.CreatorUserId,
                                            t.WorkspaceId,
                                            t.CreatedAt,
                                            t.UpdatedAt,
                                            t.ResolvedAt))
                .FirstOrDefaultAsync(cancellationToken);

            if (ticketResponse is null)
            {
                return Result<TicketDetailResponse>.Failure("The ticket was not found.");
            }

            var comments = await dbContext.TicketComments
                .Where(c => c.TicketId == ticketResponse.Id)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new TicketCommentResponse(
                    c.Id,
                    c.TicketId,
                    c.UserId,
                    c.Content,
                    c.CreatedAt,
                    c.UpdatedAt))
                .ToListAsync(cancellationToken);

            var history = await dbContext.TicketStatusChanges
                .Where(h => h.TicketId == ticketResponse.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new TicketStatusChangeResponse(
                    h.Id,
                    h.TicketId,
                    h.OldStatus,
                    h.NewStatus,
                    h.ChangedByUserId,
                    h.CreatedAt))
                .ToListAsync(cancellationToken);

            var response = new TicketDetailResponse(
                ticketResponse,
                comments,
                history);

            return Result<TicketDetailResponse>.Success(response);
        }
    }
}
