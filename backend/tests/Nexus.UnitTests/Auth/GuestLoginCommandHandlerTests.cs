using FluentAssertions;
using Moq;
using Xunit;
using Nexus.Application.Abstractions;
using Nexus.Application.Auth.Commands.GuestLogin;
using Nexus.Domain.Entities;

namespace Nexus.UnitTests.Auth;

public class GuestLoginCommandHandlerTests : TestBase
{
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock;
    private readonly GuestLogin.Handler _handler;

    public GuestLoginCommandHandlerTests()
    {
        _jwtProviderMock = new Mock<IJwtProvider>();
        _refreshTokenHasherMock = new Mock<IRefreshTokenHasher>();

        _refreshTokenHasherMock.Setup(x => x.GenerateRawToken()).Returns("raw_refresh_token");
        _refreshTokenHasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"hash_of_{s}");
        _jwtProviderMock.Setup(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(30));

        _handler = new GuestLogin.Handler(
            DbContext,
            _jwtProviderMock.Object,
            _refreshTokenHasherMock.Object);
    }

    [Fact]
    public async Task Handle_Should_IssueTokens_WhenGuestUserIsSeeded()
    {
        // Arrange — seed the well-known guest user (as DatabaseInitializer does).
        var guest = User.CreateSystem(SystemUsers.GuestUserId, SystemUsers.GuestEmail, SystemUsers.GuestUsername);
        DbContext.Users.Add(guest);
        await DbContext.SaveChangesAsync();

        _jwtProviderMock.Setup(x => x.Generate(It.Is<User>(u => u.Id == SystemUsers.GuestUserId)))
            .Returns(("jwt_token", DateTime.UtcNow.AddMinutes(15)));

        var command = new GuestLogin.Command(CreatedByIp: null, UserAgent: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Response.AccessToken.Should().Be("jwt_token");
        result.Value.Response.User.Username.Should().Be(SystemUsers.GuestUsername);
        result.Value.RefreshToken.Should().Be("raw_refresh_token");
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenGuestUserIsNotSeeded()
    {
        // Arrange — no guest user in the DB (demo seeding disabled).
        var command = new GuestLogin.Command(null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Guest access is not enabled.");
    }
}
