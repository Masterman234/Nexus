using FluentAssertions;
using Moq;
using Xunit;
using Nexus.Application.Abstractions;
using Nexus.Application.Auth.Commands.Register;
using Nexus.Application.Auth.IntegrationEvents;
using Nexus.Domain.Entities;
using MassTransit;

namespace Nexus.UnitTests.Auth;

public class RegisterUserCommandHandlerTests : TestBase
{
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly RegisterUser.Handler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _refreshTokenHasherMock = new Mock<IRefreshTokenHasher>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();

        _refreshTokenHasherMock.Setup(x => x.GenerateRawToken()).Returns("raw_refresh_token");
        _refreshTokenHasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"hash_of_{s}");
        _jwtProviderMock.Setup(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(30));

        _handler = new RegisterUser.Handler(
            DbContext,
            _passwordHasherMock.Object,
            _jwtProviderMock.Object,
            _refreshTokenHasherMock.Object,
            _publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenEmailIsUnique()
    {
        // Arrange
        var command = new RegisterUser.Command("test@nexus.com", "testuser", "password123", null, null);
        _passwordHasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed_password");
        _jwtProviderMock.Setup(x => x.Generate(It.IsAny<User>()))
            .Returns(("jwt_token", DateTime.UtcNow.AddMinutes(15)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Response.AccessToken.Should().Be("jwt_token");
        result.Value.Response.User.Username.Should().Be(command.Username);
        DbContext.Users.Should().ContainSingle(u => u.Email == command.Email);

        // First human registration → Admin bootstrap rule.
        result.Value.Response.User.Role.Should().Be(nameof(UserRole.Admin));

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<UserCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenEmailIsNotUnique()
    {
        // Arrange
        var email = "duplicate@nexus.com";
        var existingUser = User.Create(email, "existing", "hash");
        DbContext.Users.Add(existingUser);
        await DbContext.SaveChangesAsync();

        var command = new RegisterUser.Command(email, "newuser", "password123", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email is already in use.");
    }
}
