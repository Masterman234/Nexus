namespace Nexus.Application.Engineering;

public record EngineeringActivityResponse(
    List<CommitResponse> Commits,
    List<PullRequestResponse> PullRequests);

public record CommitResponse(
    Guid Id,
    string Sha,
    string Message,
    string AuthorName,
    string AuthorEmail,
    string RepositoryName,
    string Url,
    DateTime CommittedAt);

public record PullRequestResponse(
    Guid Id,
    long ExternalId,
    int Number,
    string Title,
    string Description,
    string State,
    string Url,
    string RepositoryName,
    string AuthorName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? MergedAt);
