namespace Nexus.Application.Workspaces;

public record WorkspaceResponse(Guid Id, string Name, string Description, DateTime CreatedAt);
