using Nexus.Domain.Entities;

namespace Nexus.Application.Abstractions;

public interface IJwtProvider
{
    /// <summary>
    /// Issues a fresh access token for <paramref name="user"/>.
    /// </summary>
    /// <returns>The signed JWT and the UTC instant at which it expires.</returns>
    (string Token, DateTime ExpiresAt) Generate(User user);

    /// <summary>
    /// Configured lifetime of a refresh token, used by the auth handlers when
    /// creating new <see cref="RefreshToken"/> rows.
    /// </summary>
    TimeSpan RefreshTokenLifetime { get; }
}
