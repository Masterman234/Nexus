namespace Nexus.Application.Engineering.IntegrationEvents;

/// <summary>
/// Published once per newly persisted commit. Note: a single push webhook can
/// produce many of these — subscribers should be idempotent and prepared for
/// bursts. The Smart Cross-Linking extractor (NEX-24) listens for this to
/// scan commit messages for ticket / incident references.
/// </summary>
public record CommitPushedIntegrationEvent(
    Guid CommitId,
    string Sha,
    string Message,
    string AuthorName,
    string AuthorEmail,
    string RepositoryName,
    DateTime CommittedAt);
