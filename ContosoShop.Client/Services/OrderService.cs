using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ContosoShop.Shared.DTOs;
using ContosoShop.Shared.Models;

namespace ContosoShop.Client.Services;

/// <summary>
/// Implementation of order service using HTTP client to communicate with API
/// </summary>
public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the OrderService
    /// </summary>
    /// <param name="httpClient">HTTP client for API communication</param>
    public OrderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<List<Order>> GetOrdersAsync()
    {
        var orders = await _httpClient.GetFromJsonAsync<List<Order>>("api/orders");
        return orders ?? new List<Order>();
    }

    /// <inheritdoc />
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Order>($"api/orders/{id}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReturnOrderItemsAsync(int orderId, List<ReturnItem> items)
    {
        try
        {
            // Fetch CSRF token
            var tokenResponse = await _httpClient.GetFromJsonAsync<CsrfTokenResponse>("api/auth/csrf-token");
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
            {
                return false;
            }

            // Create request body
            var request = new ReturnItemsRequest
            {
                Items = items
            };

            // Create HTTP request with CSRF token
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/orders/{orderId}/return-items")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json")
            };
            httpRequest.Headers.Add("X-CSRF-TOKEN", tokenResponse.Token);

            var response = await _httpClient.SendAsync(httpRequest);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private class CsrfTokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}
