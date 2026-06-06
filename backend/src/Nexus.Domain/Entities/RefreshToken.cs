using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

/// <summary>
/// A long-lived credential the client exchanges for a fresh access token. We store
/// only an HMAC-SHA256 hash of the raw token (see <c>IRefreshTokenHasher</c>) so a
/// DB-only leak does not yield usable tokens. Each successful refresh rotates the
/// token: the old row is marked <see cref="RevokedAt"/> and <see cref="ReplacedByTokenId"/>
/// points at the new row. Presenting a revoked-and-replaced token is treated as
/// theft-after-rotation and the whole descendant chain is revoked.
/// </summary>
public class RefreshToken : Entity<Guid>
{
    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? createdByIp,
        string? userAgent) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Set when this token is rotated; points at the replacement row.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>Client IP at issue-time. Audit only — never trusted for AuthZ.</summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>User-Agent string at issue-time. Audit only.</summary>
    public string? UserAgent { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? createdByIp = null,
        string? userAgent = null)
    {
        return new RefreshToken(Guid.NewGuid(), userId, tokenHash, expiresAt, createdByIp, userAgent);
    }

    public void Revoke()
    {
        if (IsRevoked) return;
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this token as rotated. Sets <see cref="RevokedAt"/> and links to the
    /// replacement so reuse of *this* token after rotation can be detected later.
    /// </summary>
    public void Rotate(Guid replacementTokenId)
    {
        Revoke();
        ReplacedByTokenId = replacementTokenId;
    }
}
