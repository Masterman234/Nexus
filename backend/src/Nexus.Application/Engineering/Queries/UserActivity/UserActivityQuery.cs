using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;

namespace Nexus.Application.Engineering.Queries.UserActivity;

/// <summary>
/// EF Core implementation of <see cref="IUserActivityQuery"/>.
///
/// Messages join cleanly on <c>UserId</c> — that's the real foreign key. Commits
/// and PullRequests, however, only carry a free-text <c>AuthorName</c> (and
/// <c>AuthorEmail</c> for commits) sourced from GitHub. There is no
/// <c>AuthorUserId</c> column yet, so this implementation falls back to a
/// case-insensitive match against <see cref="Nexus.Domain.Entities.User.Username"/>
/// for both, plus <see cref="Nexus.Domain.Entities.User.Email"/> for commits.
///
/// That matching is best-effort and will miss users whose GitHub login differs
/// from their Nexus username. A future ticket should backfill <c>AuthorUserId</c>
/// from the webhook payload and tighten this to an FK join — the public
/// contract on <see cref="IUserActivityQuery"/> does not change.
/// </summary>
public sealed class UserActivityQuery(IApplicationDbContext dbContext) : IUserActivityQuery
{
    public async Task<UserActivity> GetAsync(
        Guid userId,
        DateTime since,
        DateTime until,
        CancellationToken cancellationToken = default)
    {
        if (until <= since)
        {
            throw new ArgumentException("'until' must be after 'since'.", nameof(until));
        }

        var user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Username, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            // Caller-visible empty projection rather than a null/throw — keeps the
            // standup handler's branching simple ("if IsEmpty, no activity") and
            // matches how the rest of the system treats missing aggregate roots
            // returned through Result<T>.
            return new UserActivity(
                userId, DisplayName: "(unknown user)",
                since, until,
                Array.Empty<CommitActivity>(),
                Array.Empty<PullRequestActivity>(),
                Array.Empty<MessageActivity>(),
                Array.Empty<TicketActivity>());
        }

        // Lower-case once on the client and reuse — keeps EF translation simple and
        // avoids generating LOWER(...) in the WHERE clause on every row.
        var usernameLower = user.Username.ToLower();
        var emailLower = user.Email.ToLower();

        var commits = await dbContext.Commits
            .Where(c =>
                c.CommittedAt >= since && c.CommittedAt < until &&
                (c.AuthorName.ToLower() == usernameLower ||
                 c.AuthorEmail.ToLower() == emailLower))
            .OrderByDescending(c => c.CommittedAt)
            .Select(c => new CommitActivity(
                c.Id, c.Sha, c.Message, c.RepositoryName, c.CommittedAt))
            .ToListAsync(cancellationToken);

        var pullRequests = await dbContext.PullRequests
            .Where(pr =>
                pr.UpdatedAt >= since && pr.UpdatedAt < until &&
                pr.AuthorName.ToLower() == usernameLower)
            .OrderByDescending(pr => pr.UpdatedAt)
            .Select(pr => new PullRequestActivity(
                pr.Id, pr.Number, pr.Title, pr.State, pr.RepositoryName,
                pr.Url, pr.UpdatedAt, pr.MergedAt))
            .ToListAsync(cancellationToken);

        var messages = await dbContext.Messages
            .Where(m =>
                m.UserId == userId &&
                m.SentAt >= since && m.SentAt < until)
            .OrderByDescending(m => m.SentAt)
            .Select(m => new MessageActivity(
                m.Id, m.Content, m.ChannelId, m.SentAt))
            .ToListAsync(cancellationToken);

        var tickets = await dbContext.Tickets
            .Where(t =>
                (t.CreatorUserId == userId || t.AssigneeUserId == userId) &&
                t.UpdatedAt >= since && t.UpdatedAt < until)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TicketActivity(
                t.Id, t.Number, t.Title, t.Status.ToString(), t.Priority.ToString(), t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new UserActivity(
            user.Id, user.Username, since, until,
            commits, pullRequests, messages, tickets);
    }
}
