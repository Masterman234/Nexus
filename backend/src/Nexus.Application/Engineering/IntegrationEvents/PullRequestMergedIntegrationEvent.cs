namespace Nexus.Application.Engineering.IntegrationEvents;

/// <summary>
/// Published when a pull request transitions to the merged state. This is the
/// canonical "code shipped" signal in the event spine — the Postmortem
/// Assistant (EPIC-06) correlates incidents to recent merges, and the AI
/// Standup Generator (EPIC-05) uses it to highlight delivered work.
/// </summary>
public record PullRequestMergedIntegrationEvent(
    Guid PullRequestId,
    long ExternalId,
    int Number,
    string Title,
    string Url,
    string RepositoryName,
    string AuthorName,
    DateTime MergedAt);
