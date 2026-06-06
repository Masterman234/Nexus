using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;

namespace Nexus.Application.Auth.Commands.Logout;

public static class LogoutUser
{
    /// <summary>
    /// Revokes a single refresh token. We accept whatever the cookie carried,
    /// hash it, and revoke if found — silent on miss so a missing/expired cookie
    /// during logout doesn't 4xx the UI. The access token is left to expire on
    /// its own (15 min); stateless JWTs aren't revocable without a denylist.
    /// </summary>
    public record Command(string? RawRefreshToken) : IRequest<Result<bool>>;

    public class Handler(
        IApplicationDbContext dbContext,
        IRefreshTokenHasher refreshTokenHasher)
        : IRequestHandler<Command, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
            {
                return Result<bool>.Success(true);
            }

            var tokenHash = refreshTokenHasher.Hash(request.RawRefreshToken);

            var existing = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (existing is { IsRevoked: false })
            {
                existing.Revoke();
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Result<bool>.Success(true);
        }
    }
}
