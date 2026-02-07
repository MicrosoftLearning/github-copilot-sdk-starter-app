using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ContosoShop.Client.Services;

/// <summary>
/// Authentication state provider that checks authentication status via API endpoint
/// </summary>
public class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    public CookieAuthenticationStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Check authentication status by calling the user info endpoint
            var userInfo = await _httpClient.GetFromJsonAsync<UserInfo>("api/auth/user");

            if (userInfo?.IsAuthenticated == true && !string.IsNullOrEmpty(userInfo.Email))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userInfo.Email),
                    new Claim(ClaimTypes.Email, userInfo.Email)
                };

                if (!string.IsNullOrEmpty(userInfo.Name))
                {
                    claims.Add(new Claim("name", userInfo.Name));
                }

                var identity = new ClaimsIdentity(claims, "cookie");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
        }
        catch (Exception)
        {
            // If the API call fails, user is not authenticated
        }

        // Return unauthenticated user
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Notify that authentication state has changed (call after login/logout)
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private class UserInfo
    {
        public bool IsAuthenticated { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }
}
