using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;

namespace Nexus.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Workspace> Workspaces { get; }
    DbSet<Channel> Channels { get; }
    DbSet<Message> Messages { get; }
    DbSet<ExternalEvent> ExternalEvents { get; }
    DbSet<Commit> Commits { get; }
    DbSet<PullRequest> PullRequests { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketStatusChange> TicketStatusChanges { get; }
    DbSet<Incident> Incidents { get; }
    DbSet<EntityReference> EntityReferences { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
