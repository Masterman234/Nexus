using MediatR;
using MassTransit;
using Nexus.Application.Abstractions;
using Nexus.Application.Webhooks.IntegrationEvents;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;

namespace Nexus.Application.Webhooks.Commands.HandleGithubWebhook;

public static class HandleGithubWebhook
{
    public record Command(string EventType, string Payload) : IRequest<Result>;

    public class Handler(
        IApplicationDbContext dbContext,
        IPublishEndpoint publishEndpoint)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var externalEvent = ExternalEvent.Create("GitHub", request.EventType, request.Payload);

            dbContext.ExternalEvents.Add(externalEvent);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Forward to RabbitMQ for heavy processing
            await publishEndpoint.Publish(
                new GithubWebhookReceivedIntegrationEvent(request.EventType, request.Payload),
                cancellationToken);

            return Result.Success();
        }
    }
}
