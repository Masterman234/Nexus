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
    public record Command(string Email, string Username, string Password) : IRequest<Result<AuthResponse>>;

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
        IPublishEndpoint publishEndpoint)
        : IRequestHandler<Command, Result<AuthResponse>>
    {
        public async Task<Result<AuthResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (await dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            {
                return Result<AuthResponse>.Failure("Email is already in use.");
            }

            var user = User.Create(
                request.Email,
                request.Username,
                passwordHasher.Hash(request.Password));

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Publish integration event to RabbitMQ
            await publishEndpoint.Publish(
                new UserCreatedIntegrationEvent(user.Id, user.Email, user.Username),
                cancellationToken);

            var token = jwtProvider.Generate(user);

            return Result<AuthResponse>.Success(new AuthResponse(
                token,
                new UserResponse(user.Id, user.Email, user.Username)));
        }
    }
}
