using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    private User(Guid id, string email, string username, string passwordHash) : base(id)
    {
        Email = email;
        Username = username;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    public string Email { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static User Create(string email, string username, string passwordHash)
    {
        return new User(Guid.NewGuid(), email, username, passwordHash);
    }

    /// <summary>
    /// Creates a system (non-human) user with a caller-supplied deterministic id.
    /// Used for bot accounts that must be safely upserted on startup. The
    /// passwordHash is set to a non-loginable sentinel so this account cannot
    /// authenticate via the normal login flow.
    /// </summary>
    public static User CreateSystem(Guid id, string email, string username)
    {
        return new User(id, email, username, passwordHash: "!SYSTEM-ACCOUNT-NOT-LOGINABLE!");
    }
}
