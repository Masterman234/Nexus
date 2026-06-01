using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Nexus.Application.Abstractions;
using Nexus.Application.Channels;
using Nexus.Application.Webhooks.Consumers;
using Nexus.Application.Webhooks.IntegrationEvents;
using Nexus.Domain.Entities;
using Xunit;

namespace Nexus.UnitTests.Webhooks;

public class GithubWebhookConsumerTests : TestBase
{
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly GithubWebhookConsumer _consumer;

    public GithubWebhookConsumerTests()
    {
        _chatServiceMock = new Mock<IChatService>();
        _configurationMock = new Mock<IConfiguration>();
        _consumer = new GithubWebhookConsumer(DbContext, _chatServiceMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task Consume_PushEvent_ShouldCreateCommits()
    {
        // Arrange
        var botUser = User.CreateSystem(SystemUsers.GithubBotId, SystemUsers.GithubBotEmail, SystemUsers.GithubBotUsername);
        DbContext.Users.Add(botUser);

        var workspace = Workspace.Create("test workspace", "test description", botUser.Id);
        DbContext.Workspaces.Add(workspace);

        var channel = Channel.Create("general", "General channel", workspace.Id);
        DbContext.Channels.Add(channel);
        await DbContext.SaveChangesAsync();

        _configurationMock.Setup(x => x["Webhook:GithubTargetChannelId"]).Returns(channel.Id.ToString());

        var payload = @"
        {
            ""repository"": { ""full_name"": ""test/repo"" },
            ""pusher"": { ""name"": ""testuser"" },
            ""commits"": [
                {
                    ""id"": ""sha123"",
                    ""message"": ""feat: test commit"",
                    ""timestamp"": ""2026-06-01T12:00:00Z"",
                    ""author"": { ""name"": ""Test Author"", ""email"": ""test@example.com"" }
                }
            ]
        }";

        var @event = new GithubWebhookReceivedIntegrationEvent("push", payload);
        var contextMock = new Mock<ConsumeContext<GithubWebhookReceivedIntegrationEvent>>();
        contextMock.Setup(x => x.Message).Returns(@event);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        var commit = await DbContext.Commits.FirstOrDefaultAsync(c => c.Sha == "sha123");
        commit.Should().NotBeNull();
        commit!.Message.Should().Be("feat: test commit");
        commit.AuthorName.Should().Be("Test Author");
        commit.RepositoryName.Should().Be("test/repo");
    }

    [Fact]
    public async Task Consume_PullRequestEvent_ShouldCreatePullRequest()
    {
        // Arrange
        var botUser = User.CreateSystem(SystemUsers.GithubBotId, SystemUsers.GithubBotEmail, SystemUsers.GithubBotUsername);
        DbContext.Users.Add(botUser);

        var workspace = Workspace.Create("test workspace", "test description", botUser.Id);
        DbContext.Workspaces.Add(workspace);

        var channel = Channel.Create("general", "General channel", workspace.Id);
        DbContext.Channels.Add(channel);
        await DbContext.SaveChangesAsync();

        _configurationMock.Setup(x => x["Webhook:GithubTargetChannelId"]).Returns(channel.Id.ToString());

        var payload = @"
        {
            ""action"": ""opened"",
            ""repository"": { ""full_name"": ""test/repo"" },
            ""pull_request"": {
                ""id"": 12345,
                ""number"": 1,
                ""title"": ""Test PR"",
                ""body"": ""PR Description"",
                ""state"": ""open"",
                ""html_url"": ""https://github.com/test/repo/pull/1"",
                ""user"": { ""login"": ""authoruser"" },
                ""created_at"": ""2026-06-01T12:00:00Z"",
                ""updated_at"": ""2026-06-01T12:00:00Z""
            }
        }";

        var @event = new GithubWebhookReceivedIntegrationEvent("pull_request", payload);
        var contextMock = new Mock<ConsumeContext<GithubWebhookReceivedIntegrationEvent>>();
        contextMock.Setup(x => x.Message).Returns(@event);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        var pr = await DbContext.PullRequests.FirstOrDefaultAsync(p => p.ExternalId == 12345);
        pr.Should().NotBeNull();
        pr!.Title.Should().Be("Test PR");
        pr.AuthorName.Should().Be("authoruser");
        pr.State.Should().Be("open");
    }
}
