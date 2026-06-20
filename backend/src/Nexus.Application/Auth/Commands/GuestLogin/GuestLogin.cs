using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Auth.Commands.GuestLogin;

/// <summary>
/// One-click "Try the demo" login. Issues tokens for the well-known seeded guest
/// user (<see cref="SystemUsers.GuestUserId"/>) without any credential check — the
/// guest account has no password by design. Only succeeds when the guest user has
/// been seeded (i.e. on the demo deployment with Seed:Demo enabled), so a normal
/// production deployment that never seeds the guest simply returns a failure.
/// </summary>
public static class GuestLogin
{
    public record Command(string? CreatedByIp, string? UserAgent)
        : IRequest<Result<AuthTokensResult>>;

    public class Handler(
        IApplicationDbContext dbContext,
        IJwtProvider jwtProvider,
        IRefreshTokenHasher refreshTokenHasher)
        : IRequestHandler<Command, Result<AuthTokensResult>>
    {
        public async Task<Result<AuthTokensResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == SystemUsers.GuestUserId, cancellationToken);

            if (user is null)
            {
                // Demo seeding is off (or hasn't run) — no guest to log in as.
                return Result<AuthTokensResult>.Failure("Guest access is not enabled.");
            }

            var (accessToken, accessExpiresAt) = jwtProvider.Generate(user);

            var rawRefreshToken = refreshTokenHasher.GenerateRawToken();
            var refreshExpiresAt = DateTime.UtcNow.Add(jwtProvider.RefreshTokenLifetime);
            var refreshToken = RefreshToken.Create(
                user.Id,
                refreshTokenHasher.Hash(rawRefreshToken),
                refreshExpiresAt,
                request.CreatedByIp,
                request.UserAgent);

            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<AuthTokensResult>.Success(new AuthTokensResult(
                new AuthResponse(
                    accessToken,
                    accessExpiresAt,
                    new UserResponse(user.Id, user.Email, user.Username, user.Role.ToString())),
                rawRefreshToken,
                refreshExpiresAt));
        }
    }
}
