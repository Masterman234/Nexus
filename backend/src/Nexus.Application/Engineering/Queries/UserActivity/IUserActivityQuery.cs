namespace Nexus.Application.Engineering.Queries.UserActivity;

/// <summary>
/// Cross-context projection of a single user's activity (Commits + PullRequests
/// + Messages) within a time window. Read-only — no domain mutation. This is
/// the SQL-level backbone of EPIC-05 (AI Standup) and surfaces a stable
/// contract regardless of which contexts contribute data; adding a new source
/// (e.g. Incidents from EPIC-06) only requires extending <see cref="UserActivity"/>
/// and the implementation, not callers.
/// </summary>
public interface IUserActivityQuery
{
    Task<UserActivity> GetAsync(
        Guid userId,
        DateTime since,
        DateTime until,
        CancellationToken cancellationToken = default);
}
