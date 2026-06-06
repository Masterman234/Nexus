using FluentAssertions;
using Moq;
using Xunit;
using Nexus.Application.Abstractions;
using Nexus.Application.Auth.Commands.Login;
using Nexus.Domain.Entities;

namespace Nexus.UnitTests.Auth;

public class LoginUserCommandHandlerTests : TestBase
{
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock;
    private readonly LoginUser.Handler _handler;

    public LoginUserCommandHandlerTests()
    {
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _refreshTokenHasherMock = new Mock<IRefreshTokenHasher>();

        // Default refresh-token plumbing — generate a deterministic raw token and
        // hash. Tests that care about the value override; the rest just need the
        // handler to be able to persist a row.
        _refreshTokenHasherMock.Setup(x => x.GenerateRawToken()).Returns("raw_refresh_token");
        _refreshTokenHasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(s => $"hash_of_{s}");
        _jwtProviderMock.Setup(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(30));

        _handler = new LoginUser.Handler(
            DbContext,
            _passwordHasherMock.Object,
            _jwtProviderMock.Object,
            _refreshTokenHasherMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "test@nexus.com";
        var password = "password123";
        var passwordHash = "hashed_password";
        var user = User.Create(email, "testuser", passwordHash);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var command = new LoginUser.Command(email, password, CreatedByIp: null, UserAgent: null);
        _passwordHasherMock.Setup(x => x.Verify(password, passwordHash)).Returns(true);
        _jwtProviderMock.Setup(x => x.Generate(user))
            .Returns(("jwt_token", DateTime.UtcNow.AddMinutes(15)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Response.AccessToken.Should().Be("jwt_token");
        result.Value.Response.User.Email.Should().Be(email);
        result.Value.RefreshToken.Should().Be("raw_refresh_token");
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange — the handler now runs a dummy Verify even when the user is
        // absent (timing-attack mitigation), so the mock must answer false.
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var command = new LoginUser.Command("nonexistent@nexus.com", "password", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPasswordIsIncorrect()
    {
        // Arrange
        var email = "test@nexus.com";
        var user = User.Create(email, "testuser", "correct_hash");
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var command = new LoginUser.Command(email, "wrong_password", null, null);
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid credentials.");
    }
}
