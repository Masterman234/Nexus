namespace Nexus.Application.Engineering.IntegrationEvents;

/// <summary>
/// Published when a new pull request is created on a connected repository.
/// Cross-context subscribers (AI standup, smart cross-linking, incident
/// correlation) react to this without polling the PullRequests table.
/// </summary>
public record PullRequestOpenedIntegrationEvent(
    Guid PullRequestId,
    long ExternalId,
    int Number,
    string Title,
    string Description,
    string Url,
    string RepositoryName,
    string AuthorName,
    DateTime CreatedAt);
