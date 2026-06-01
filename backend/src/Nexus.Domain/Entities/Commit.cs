using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class Commit : AggregateRoot<Guid>
{
    private Commit(
        Guid id,
        string sha,
        string message,
        string authorName,
        string authorEmail,
        string repositoryName,
        DateTime committedAt) : base(id)
    {
        Sha = sha;
        Message = message;
        AuthorName = authorName;
        AuthorEmail = authorEmail;
        RepositoryName = repositoryName;
        CommittedAt = committedAt;
    }

    public string Sha { get; private set; }
    public string Message { get; private set; }
    public string AuthorName { get; private set; }
    public string AuthorEmail { get; private set; }
    public string RepositoryName { get; private set; }
    public DateTime CommittedAt { get; private set; }

    public static Commit Create(
        string sha,
        string message,
        string authorName,
        string authorEmail,
        string repositoryName,
        DateTime committedAt)
    {
        return new Commit(
            Guid.NewGuid(),
            sha,
            message,
            authorName,
            authorEmail,
            repositoryName,
            committedAt);
    }
}
