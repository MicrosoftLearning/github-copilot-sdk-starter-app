using System.Net;
using System.Net.Http.Json;
using ContosoShop.Shared.Models;
using Xunit;

namespace ContosoShop.Server.Tests.Integration;

/// <summary>
/// Integration tests for CSRF protection (FR-014 to FR-017)
/// Tests that state-changing operations require valid anti-forgery tokens
/// </summary>
public class CsrfProtectionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CsrfProtectionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCsrfToken_WhenAuthenticated_ReturnsToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@contoso.com", "Password123!");

        // Act
        var response = await client.GetAsync("/api/auth/csrf-token");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(token);
        Assert.NotEqual("\"\"", token);
    }

    [Fact]
    public async Task GetCsrfToken_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/csrf-token");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReturnOrder_WithoutCsrfToken_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@contoso.com", "Password123!");

        // Act - Try to return order without CSRF token
        var response = await client.PostAsync("/api/orders/1/return", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReturnOrder_WithInvalidCsrfToken_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@contoso.com", "Password123!");

        // Act - Try to return order with invalid CSRF token
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders/1/return");
        request.Headers.Add("X-CSRF-TOKEN", "invalid-token-12345");
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReturnOrder_WithValidCsrfToken_ProcessesRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@contoso.com", "Password123!");

        // Get valid CSRF token
        var csrfResponse = await client.GetAsync("/api/auth/csrf-token");
        var csrfToken = await csrfResponse.Content.ReadAsStringAsync();
        csrfToken = csrfToken.Trim('"');

        // Act - Try to return delivered order with valid CSRF token
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders/1/return");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        var response = await client.SendAsync(request);

        // Assert - Should succeed or return BadRequest (if order status doesn't allow return)
        // We just verify it's not 400 due to missing CSRF token
        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReturnOrder_CsrfTokenFromDifferentSession_ReturnsBadRequest()
    {
        // Arrange - Create two separate clients (different sessions)
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        await LoginAsync(client1, "john@contoso.com", "Password123!");
        await LoginAsync(client2, "jane@contoso.com", "Password123!");

        // Get CSRF token from client1 (John's session)
        var csrfResponse = await client1.GetAsync("/api/auth/csrf-token");
        var csrfToken = await csrfResponse.Content.ReadAsStringAsync();
        csrfToken = csrfToken.Trim('"');

        // Act - Try to use John's CSRF token in Jane's session
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders/4/return");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        var response = await client2.SendAsync(request);

        // Assert - Should fail because token is from different session
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_DoesNotRequireCsrfToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new LoginRequest
        {
            Email = "john@contoso.com",
            Password = "Password123!"
        };

        // Act - Login without CSRF token (should work)
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task LoginAsync(HttpClient client, string email, string password)
    {
        var loginRequest = new LoginRequest { Email = email, Password = password };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        // Extract and store authentication cookie
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                client.DefaultRequestHeaders.Add("Cookie", cookie);
            }
        }
    }
}
