using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;
using MassTransit;
using Nexus.Application.Auth.IntegrationEvents;

namespace Nexus.Application.Auth.Commands.Register;

public static class RegisterUser
{
    public record Command(string Email, string Username, string Password, string? CreatedByIp, string? UserAgent)
        : IRequest<Result<AuthTokensResult>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        }
    }

    public class Handler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IRefreshTokenHasher refreshTokenHasher,
        IPublishEndpoint publishEndpoint)
        : IRequestHandler<Command, Result<AuthTokensResult>>
    {
        public async Task<Result<AuthTokensResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (await dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            {
                return Result<AuthTokensResult>.Failure("Email is already in use.");
            }

            // Bootstrap the system: the very first human registration becomes Admin so
            // there is always at least one account that can promote others. Excludes
            // seeded bot accounts (github-bot, nexus-bot) which exist before any human.
            var isFirstHuman = !await dbContext.Users
                .AnyAsync(u => u.Id != SystemUsers.GithubBotId && u.Id != SystemUsers.NexusBotId, cancellationToken);

            var user = User.Create(
                request.Email,
                request.Username,
                passwordHasher.Hash(request.Password),
                role: isFirstHuman ? UserRole.Admin : UserRole.Member);

            dbContext.Users.Add(user);

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

            // Publish integration event to RabbitMQ
            await publishEndpoint.Publish(
                new UserCreatedIntegrationEvent(user.Id, user.Email, user.Username),
                cancellationToken);

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
