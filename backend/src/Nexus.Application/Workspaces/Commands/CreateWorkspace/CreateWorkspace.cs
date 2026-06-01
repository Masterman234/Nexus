using FluentValidation;
using MediatR;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Workspaces.Commands.CreateWorkspace;

public static class CreateWorkspace
{
    public record Command(string Name, string Description, Guid OwnerId) : IRequest<Result<WorkspaceResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
            RuleFor(x => x.OwnerId).NotEmpty();
        }
    }

    public class Handler(IApplicationDbContext dbContext)
        : IRequestHandler<Command, Result<WorkspaceResponse>>
    {
        public async Task<Result<WorkspaceResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var workspace = Workspace.Create(request.Name, request.Description, request.OwnerId);

            dbContext.Workspaces.Add(workspace);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<WorkspaceResponse>.Success(new WorkspaceResponse(
                workspace.Id,
                workspace.Name,
                workspace.Description,
                workspace.CreatedAt));
        }
    }
}
