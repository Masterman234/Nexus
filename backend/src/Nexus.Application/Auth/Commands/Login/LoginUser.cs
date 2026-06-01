using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;

namespace Nexus.Application.Auth.Commands.Login;

public static class LoginUser
{
    public record Command(string Email, string Password) : IRequest<Result<AuthResponse>>;

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
        IJwtProvider jwtProvider)
        : IRequestHandler<Command, Result<AuthResponse>>
    {
        public async Task<Result<AuthResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Result<AuthResponse>.Failure("Invalid credentials.");
            }

            var token = jwtProvider.Generate(user);

            return Result<AuthResponse>.Success(new AuthResponse(
                token,
                new UserResponse(user.Id, user.Email, user.Username)));
        }
    }
}
