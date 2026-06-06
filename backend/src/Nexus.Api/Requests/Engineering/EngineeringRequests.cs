using Nexus.Domain.Entities;

namespace Nexus.Api.Requests.Engineering;

public record DeclareIncidentRequest(
    string Title, 
    string Description, 
    IncidentSeverity Severity, 
    Guid WorkspaceId);

public record ResolveIncidentRequest(Guid ChannelId);
