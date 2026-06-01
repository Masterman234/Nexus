namespace Nexus.Application.Webhooks.IntegrationEvents;

public record GithubWebhookReceivedIntegrationEvent(string EventType, string Payload);
