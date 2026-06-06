namespace Nexus.Application.Engineering.Queries.UserActivity;

/// <summary>
/// Per-user cross-context activity within a time window. This is the canonical
/// projection feeding EPIC-05 (AI Standup) and any future "what did X do?"
/// surface. The shape is deliberately flat — handlers serialise this straight
/// into an LLM context block, so adding nested objects costs prompt tokens.
/// </summary>
public sealed record UserActivity(
    Guid UserId,
    string DisplayName,
    DateTime Since,
    DateTime Until,
    IReadOnlyList<CommitActivity> Commits,
    IReadOnlyList<PullRequestActivity> PullRequests,
    IReadOnlyList<MessageActivity> Messages,
    IReadOnlyList<TicketActivity> Tickets)
{
    public bool IsEmpty => Commits.Count == 0 && PullRequests.Count == 0 && Messages.Count == 0 && Tickets.Count == 0;
}

public sealed record CommitActivity(
    Guid Id,
    string Sha,
    string Message,
    string RepositoryName,
    DateTime CommittedAt);

public sealed record PullRequestActivity(
    Guid Id,
    int Number,
    string Title,
    string State,
    string RepositoryName,
    string Url,
    DateTime UpdatedAt,
    DateTime? MergedAt);

public sealed record MessageActivity(
    Guid Id,
    string Content,
    Guid ChannelId,
    DateTime SentAt);

public sealed record TicketActivity(
    Guid Id,
    int Number,
    string Title,
    string Status,
    string Priority,
    DateTime UpdatedAt);
