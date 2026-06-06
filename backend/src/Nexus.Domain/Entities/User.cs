using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    private User(Guid id, string email, string username, string passwordHash, UserRole role) : base(id)
    {
        Email = email;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public string Email { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static User Create(string email, string username, string passwordHash, UserRole role = UserRole.Member)
    {
        return new User(Guid.NewGuid(), email, username, passwordHash, role);
    }

    /// <summary>
    /// Creates a system (non-human) user with a caller-supplied deterministic id.
    /// Used for bot accounts that must be safely upserted on startup. The
    /// passwordHash is set to a non-loginable sentinel so this account cannot
    /// authenticate via the normal login flow.
    /// </summary>
    public static User CreateSystem(Guid id, string email, string username)
    {
        return new User(id, email, username, passwordHash: "!SYSTEM-ACCOUNT-NOT-LOGINABLE!", role: UserRole.Member);
    }

    /// <summary>
    /// Promotes/demotes the user. Only callers wrapped by an admin-only policy
    /// should invoke this — the entity itself does not enforce who may call it.
    /// </summary>
    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }
}
