using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Application.Auth;
using Nexus.SharedKernel;

namespace Nexus.Application.Auth.Queries.GetUsers;

public static class GetUsers
{
    public class Query : IRequest<Result<List<UserResponse>>>;

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Query, Result<List<UserResponse>>>
    {
        public async Task<Result<List<UserResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var users = await dbContext.Users
                .OrderBy(u => u.Username)
                .Select(u => new UserResponse(u.Id, u.Email, u.Username, u.Role.ToString()))
                .ToListAsync(cancellationToken);

            return Result<List<UserResponse>>.Success(users);
        }
    }
}
