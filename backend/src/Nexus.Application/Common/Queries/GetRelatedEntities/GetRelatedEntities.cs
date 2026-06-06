using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Common.Queries.GetRelatedEntities;

public record RelatedEntityResponse(
    Guid EntityId,
    string EntityType,
    string DisplayTitle,
    string ReferenceValue,
    DateTime CreatedAt,
    string Relationship); // "Mentions" or "MentionedBy"

public static class GetRelatedEntities
{
    public record Query(Guid EntityId) : IRequest<Result<List<RelatedEntityResponse>>>;

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Query, Result<List<RelatedEntityResponse>>>
    {
        public async Task<Result<List<RelatedEntityResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = new List<RelatedEntityResponse>();

            // 1. Find references where this entity is the SOURCE (What does this entity mention?)
            var mentions = await dbContext.EntityReferences
                .Where(er => er.SourceEntityId == request.EntityId)
                .ToListAsync(cancellationToken);

            foreach (var r in mentions)
            {
                var title = await ResolveDisplayTitleAsync(r.TargetEntityId, r.TargetEntityType, r.TargetValue, cancellationToken);
                results.Add(new RelatedEntityResponse(
                    r.TargetEntityId ?? Guid.Empty,
                    r.TargetEntityType,
                    title,
                    r.TargetValue,
                    r.CreatedAt,
                    "Mentions"));
            }

            // 2. Find references where this entity is the TARGET (What mentions this entity?)
            var mentionedBy = await dbContext.EntityReferences
                .Where(er => er.TargetEntityId == request.EntityId)
                .ToListAsync(cancellationToken);

            foreach (var r in mentionedBy)
            {
                var title = await ResolveDisplayTitleAsync(r.SourceEntityId, r.SourceEntityType, null, cancellationToken);
                results.Add(new RelatedEntityResponse(
                    r.SourceEntityId,
                    r.SourceEntityType,
                    title,
                    GetReferenceValue(r.SourceEntityType), // Removed r.SourceEntityId
                    r.CreatedAt,
                    "MentionedBy"));
            }

            return Result<List<RelatedEntityResponse>>.Success(results.OrderByDescending(x => x.CreatedAt).ToList());
        }

        private async Task<string> ResolveDisplayTitleAsync(Guid? id, string type, string? fallbackValue, CancellationToken ct)
        {
            if (id == null || id == Guid.Empty) return fallbackValue ?? "Unknown Entity";

            return type switch
            {
                nameof(Ticket) => await dbContext.Tickets.Where(t => t.Id == id).Select(t => t.Title).FirstOrDefaultAsync(ct) ?? "Deleted Ticket",
                nameof(PullRequest) => await dbContext.PullRequests.Where(pr => pr.Id == id).Select(pr => pr.Title).FirstOrDefaultAsync(ct) ?? "Deleted PR",
                nameof(Commit) => await dbContext.Commits.Where(c => c.Id == id).Select(c => c.Message).FirstOrDefaultAsync(ct) ?? "Deleted Commit",
                nameof(Message) => await dbContext.Messages.Where(m => m.Id == id).Select(m => m.Content).FirstOrDefaultAsync(ct) ?? "Deleted Message",
                nameof(TicketComment) => "Ticket Comment",
                _ => fallbackValue ?? "Unknown Entity"
            };
        }

        private string GetReferenceValue(string type)
        {
            // For now, return a placeholder or type name. 
            // In a real system, we might want a "IEntityLookup" service to get "NEX-123" from an ID.
            return type;
        }
    }
}
