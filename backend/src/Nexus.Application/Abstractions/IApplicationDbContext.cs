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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
