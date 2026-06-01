using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class PullRequest : AggregateRoot<Guid>
{
    private PullRequest(
        Guid id,
        long externalId,
        int number,
        string title,
        string description,
        string state,
        string url,
        string repositoryName,
        string authorName,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? mergedAt) : base(id)
    {
        ExternalId = externalId;
        Number = number;
        Title = title;
        Description = description;
        State = state;
        Url = url;
        RepositoryName = repositoryName;
        AuthorName = authorName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MergedAt = mergedAt;
    }

    public long ExternalId { get; private set; }
    public int Number { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string State { get; private set; }
    public string Url { get; private set; }
    public string RepositoryName { get; private set; }
    public string AuthorName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? MergedAt { get; private set; }

    public static PullRequest Create(
        long externalId,
        int number,
        string title,
        string description,
        string state,
        string url,
        string repositoryName,
        string authorName,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? mergedAt)
    {
        return new PullRequest(
            Guid.NewGuid(),
            externalId,
            number,
            title,
            description,
            state,
            url,
            repositoryName,
            authorName,
            createdAt,
            updatedAt,
            mergedAt);
    }

    public void Update(string title, string description, string state, DateTime updatedAt, DateTime? mergedAt)
    {
        Title = title;
        Description = description;
        State = state;
        UpdatedAt = updatedAt;
        MergedAt = mergedAt;
    }
}
