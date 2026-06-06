using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nexus.Application.Auth;
using Xunit;

namespace Nexus.IntegrationTests.Auth;

public class AuthTests : IntegrationTestBase
{
    // Mirrors AuthController's request DTOs without taking a project reference on
    // the API project — these tests speak to it over HTTP.
    private record RegisterRequest(string Email, string Username, string Password);
    private record LoginRequest(string Email, string Password);

    [Fact]
    public async Task Register_Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest("integration@test.com", "intuser", "password123");

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with {response.StatusCode}: {error}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.User.Username.Should().Be(request.Username);

        // Refresh token must arrive as an HttpOnly cookie, never in the body.
        response.Headers.Should().ContainKey("Set-Cookie");
    }

    [Fact]
    public async Task Login_Should_ReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "login@test.com";
        var password = "password123";
        await Client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "loginuser", password));

        var loginRequest = new LoginRequest(email, password);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with {response.StatusCode}: {error}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
    }
}
