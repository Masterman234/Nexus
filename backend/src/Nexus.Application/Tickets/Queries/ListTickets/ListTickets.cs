using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Tickets.Queries.ListTickets;

public static class ListTickets
{
    public record Query(
        Guid WorkspaceId,
        Guid? AssigneeUserId = null,
        TicketStatus? Status = null) : IRequest<Result<List<TicketResponse>>>;

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Query, Result<List<TicketResponse>>>
    {
        public async Task<Result<List<TicketResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = dbContext.Tickets
                .Where(t => t.WorkspaceId == request.WorkspaceId);

            if (request.AssigneeUserId.HasValue)
            {
                query = query.Where(t => t.AssigneeUserId == request.AssigneeUserId.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(t => t.Status == request.Status.Value);
            }

            var tickets = await (from t in query
                                join u in dbContext.Users on t.AssigneeUserId equals u.Id into users
                                from u in users.DefaultIfEmpty()
                                orderby t.CreatedAt descending
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
                .ToListAsync(cancellationToken);

            return Result<List<TicketResponse>>.Success(tickets);
        }
    }
}
