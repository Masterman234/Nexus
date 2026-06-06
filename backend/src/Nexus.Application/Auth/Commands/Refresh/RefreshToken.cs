using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;
using DomainRefreshToken = Nexus.Domain.Entities.RefreshToken;

namespace Nexus.Application.Auth.Commands.Refresh;

public static class RefreshToken
{
    public record Command(string RawRefreshToken, string? CreatedByIp, string? UserAgent)
        : IRequest<Result<AuthTokensResult>>;

    public class Handler(
        IApplicationDbContext dbContext,
        IJwtProvider jwtProvider,
        IRefreshTokenHasher refreshTokenHasher)
        : IRequestHandler<Command, Result<AuthTokensResult>>
    {
        public async Task<Result<AuthTokensResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
            {
                return Result<AuthTokensResult>.Failure("Refresh token missing.");
            }

            var tokenHash = refreshTokenHasher.Hash(request.RawRefreshToken);

            var existing = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (existing is null)
            {
                // Hash didn't match anything — caller forged or guessed. Nothing to revoke.
                return Result<AuthTokensResult>.Failure("Invalid refresh token.");
            }

            // Reuse-after-rotation: the presented token was already rotated. Treat as
            // theft and burn the entire family for this user so the attacker can't
            // also redeem the *new* token they may have observed. The legitimate user
            // is forced to re-login, which is the correct behavior under suspicion.
            if (existing.IsRevoked)
            {
                await RevokeAllActiveForUserAsync(existing.UserId, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result<AuthTokensResult>.Failure("Refresh token reuse detected. Please log in again.");
            }

            if (existing.IsExpired)
            {
                return Result<AuthTokensResult>.Failure("Refresh token expired.");
            }

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);

            if (user is null)
            {
                return Result<AuthTokensResult>.Failure("User no longer exists.");
            }

            // Rotate: issue a fresh token, mark the old one Replaced.
            var rawNew = refreshTokenHasher.GenerateRawToken();
            var newExpiresAt = DateTime.UtcNow.Add(jwtProvider.RefreshTokenLifetime);
            var replacement = DomainRefreshToken.Create(
                user.Id,
                refreshTokenHasher.Hash(rawNew),
                newExpiresAt,
                request.CreatedByIp,
                request.UserAgent);

            dbContext.RefreshTokens.Add(replacement);
            existing.Rotate(replacement.Id);

            var (accessToken, accessExpiresAt) = jwtProvider.Generate(user);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<AuthTokensResult>.Success(new AuthTokensResult(
                new AuthResponse(
                    accessToken,
                    accessExpiresAt,
                    new UserResponse(user.Id, user.Email, user.Username, user.Role.ToString())),
                rawNew,
                newExpiresAt));
        }

        private async Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var active = await dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var t in active)
            {
                t.Revoke();
            }
        }
    }
}
