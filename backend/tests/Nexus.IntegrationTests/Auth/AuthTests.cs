using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nexus.Application.Auth;
using Nexus.Application.Auth.Commands.Register;
using Xunit;

namespace Nexus.IntegrationTests.Auth;

public class AuthTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var command = new RegisterUser.Command("integration@test.com", "intuser", "password123");

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", command);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with {response.StatusCode}: {error}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.User.Username.Should().Be(command.Username);
    }

    [Fact]
    public async Task Login_Should_ReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "login@test.com";
        var password = "password123";
        var registerCommand = new RegisterUser.Command(email, "loginuser", password);
        await Client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);

        var loginCommand = new { Email = email, Password = password };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with {response.StatusCode}: {error}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
    }
}
