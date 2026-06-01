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
    private readonly LoginUser.Handler _handler;

    public LoginUserCommandHandlerTests()
    {
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _handler = new LoginUser.Handler(DbContext, _passwordHasherMock.Object, _jwtProviderMock.Object);
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

        var command = new LoginUser.Command(email, password);
        _passwordHasherMock.Setup(x => x.Verify(password, passwordHash)).Returns(true);
        _jwtProviderMock.Setup(x => x.Generate(user)).Returns("jwt_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt_token");
        result.Value.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        var command = new LoginUser.Command("nonexistent@nexus.com", "password");

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

        var command = new LoginUser.Command(email, "wrong_password");
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid credentials.");
    }
}
