using FluentValidation;
using MassTransit;
using MediatR;
using Nexus.Application.Abstractions;
using Nexus.Application.Channels.IntegrationEvents;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Engineering.Commands.DeclareIncident;

public static class DeclareIncident
{
    public record Command(
        string Title,
        string Description,
        IncidentSeverity Severity,
        Guid DeclaredByUserId,
        Guid WorkspaceId) : IRequest<Result<DeclareIncidentResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Severity).IsInEnum();
            RuleFor(x => x.DeclaredByUserId).NotEmpty();
            RuleFor(x => x.WorkspaceId).NotEmpty();
        }
    }

    public class Handler(IApplicationDbContext dbContext, IPublishEndpoint publishEndpoint)
        : IRequestHandler<Command, Result<DeclareIncidentResponse>>
    {
        public async Task<Result<DeclareIncidentResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Create a dedicated channel for the incident
            var channelName = $"inc-{Guid.NewGuid().ToString().Substring(0, 4)}-{request.Title.ToLowerInvariant().Replace(" ", "-").Replace(Environment.NewLine, "")}";
            if (channelName.Length > 100) channelName = channelName.Substring(0, 100);
            
            var dedicatedChannel = Channel.Create(
                channelName,
                $"Dedicated channel for incident: {request.Title}",
                request.WorkspaceId);

            dbContext.Channels.Add(dedicatedChannel);

            var incident = Incident.Create(
                request.Title,
                request.Description,
                request.Severity,
                request.DeclaredByUserId,
                request.WorkspaceId,
                dedicatedChannel.Id);

            dbContext.Incidents.Add(incident);

            await dbContext.SaveChangesAsync(cancellationToken);

            await publishEndpoint.Publish(new ChannelCreatedIntegrationEvent(
                dedicatedChannel.Id,
                dedicatedChannel.Name,
                dedicatedChannel.Description,
                dedicatedChannel.WorkspaceId,
                dedicatedChannel.CreatedAt), cancellationToken);

            return Result<DeclareIncidentResponse>.Success(new DeclareIncidentResponse(
                incident.Id,
                incident.Title,
                incident.Description,
                incident.Status,
                incident.Severity,
                incident.DeclaredByUserId,
                incident.WorkspaceId,
                incident.DedicatedChannelId,
                incident.CreatedAt));
        }
    }
}

public record DeclareIncidentResponse(
    Guid Id,
    string Title,
    string Description,
    IncidentStatus Status,
    IncidentSeverity Severity,
    Guid DeclaredByUserId,
    Guid WorkspaceId,
    Guid? DedicatedChannelId,
    DateTime CreatedAt);
