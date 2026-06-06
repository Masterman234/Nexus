using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Auth.Commands.Login;

public static class LoginUser
{
    public record Command(string Email, string Password, string? CreatedByIp, string? UserAgent)
        : IRequest<Result<AuthTokensResult>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class Handler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IRefreshTokenHasher refreshTokenHasher)
        : IRequestHandler<Command, Result<AuthTokensResult>>
    {
        public async Task<Result<AuthTokensResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            // Verify regardless of user existence to keep timing roughly uniform.
            // BCrypt.Verify against a known-good dummy hash burns ~equivalent CPU
            // so attackers can't enumerate emails via response-time differences.
            var passwordOk = user is not null
                ? passwordHasher.Verify(request.Password, user.PasswordHash)
                : passwordHasher.Verify(request.Password, DummyHash);

            if (user is null || !passwordOk)
            {
                return Result<AuthTokensResult>.Failure("Invalid credentials.");
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

        // Pre-computed BCrypt hash of an arbitrary fixed string. Verifying against
        // this when the user isn't found takes the same ~100ms as a real check,
        // closing a timing oracle on email enumeration.
        private const string DummyHash = "$2a$11$ZcVHEGmYsW7t8L0M0pXJce/JIM4G8aLh8bWlYVQ1KQXWXgJ6QH/qK";
    }
}
