using System.Net;
using System.Net.Http.Json;
using ContosoShop.Shared.Models;
using Xunit;

namespace ContosoShop.Server.Tests.Integration;

/// <summary>
/// Integration tests for authorization functionality (FR-009 to FR-013)
/// Tests that users can only access their own data
/// </summary>
public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOrders_AsJohn_ReturnsOnlyJohnsOrders()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@contoso.com", "Password123!");

        // Act
        var response = await client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
        Assert.NotNull(orders);
        Assert.Equal(3, orders.Count); // John has 3 orders
        Assert.All(orders, order => Assert.Equal("john@contoso.com", order.User?.Email ?? "john@contoso.com"));
    }

    [Fact]
    public async Task GetOrders_AsJane_ReturnsOnlyJanesOrders()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "jane@contoso.com", "Password123!");

        // Act
        var response = await client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
        Assert.NotNull(orders);
        Assert.Equal(2, orders.Count); // Jane has 2 orders
    }

    [Fact]
    public async Task GetOrderDetails_ForOwnOrder_ReturnsOrder()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@contoso.com", "Password123!");

        // Act - John trying to access his own order (ID 1)
        var response = await client.GetAsync("/api/orders/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(order);
        Assert.Equal(1, order.Id);
    }

    [Fact]
    public async Task GetOrderDetails_ForOtherUsersOrder_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@contoso.com", "Password123!");

        // Act - John trying to access Jane's order (ID 4)
        var response = await client.GetAsync("/api/orders/4");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReturnOrder_ForOtherUsersOrder_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@contoso.com", "Password123!");

        // Get CSRF token
        var csrfResponse = await client.GetAsync("/api/auth/csrf-token");
        var csrfToken = await csrfResponse.Content.ReadAsStringAsync();
        csrfToken = csrfToken.Trim('"');

        // Act - John trying to return Jane's order (ID 4)
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders/4/return");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
