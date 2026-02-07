using System.Net;
using Xunit;

namespace ContosoShop.Server.Tests.Integration;

/// <summary>
/// Integration tests for security headers (FR-035 to FR-037)
/// Validates that all HTTP responses include required security headers
/// </summary>
public class SecurityHeadersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityHeadersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HttpResponse_IncludesXFrameOptionsHeader()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.True(response.Headers.Contains("X-Frame-Options"), 
            "X-Frame-Options header should be present");
        var headerValue = response.Headers.GetValues("X-Frame-Options").First();
        Assert.Equal("DENY", headerValue);
    }

    [Fact]
    public async Task HttpResponse_IncludesXContentTypeOptionsHeader()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options header should be present");
        var headerValue = response.Headers.GetValues("X-Content-Type-Options").First();
        Assert.Equal("nosniff", headerValue);
    }

    [Fact]
    public async Task HttpResponse_IncludesXXssProtectionHeader()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.True(response.Headers.Contains("X-XSS-Protection"),
            "X-XSS-Protection header should be present");
        var headerValue = response.Headers.GetValues("X-XSS-Protection").First();
        Assert.Equal("1; mode=block", headerValue);
    }

    [Fact]
    public async Task HttpResponse_IncludesReferrerPolicyHeader()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.True(response.Headers.Contains("Referrer-Policy"),
            "Referrer-Policy header should be present");
        var headerValue = response.Headers.GetValues("Referrer-Policy").First();
        Assert.Equal("strict-origin-when-cross-origin", headerValue);
    }

    [Fact]
    public async Task ApiResponse_IncludesSecurityHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Test API endpoint (will return 401 but headers should still be present)
        var response = await client.GetAsync("/api/orders");

        // Assert
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/orders")]
    [InlineData("/orders")]
    [InlineData("/support")]
    public async Task AllEndpoints_IncludeSecurityHeaders(string endpoint)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert - All endpoints should have security headers
        Assert.True(response.Headers.Contains("X-Frame-Options"),
            $"Endpoint {endpoint} missing X-Frame-Options header");
        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            $"Endpoint {endpoint} missing X-Content-Type-Options header");
        Assert.True(response.Headers.Contains("X-XSS-Protection"),
            $"Endpoint {endpoint} missing X-XSS-Protection header");
        Assert.True(response.Headers.Contains("Referrer-Policy"),
            $"Endpoint {endpoint} missing Referrer-Policy header");
    }
}

/// <summary>
/// Manual HSTS testing instructions:
/// 
/// HSTS (HTTP Strict-Transport-Security) header testing requires HTTPS in production:
/// 
/// 1. Deploy application to production environment with HTTPS
/// 2. Make HTTPS request: curl -I https://yourapp.com/
/// 3. Verify response includes: Strict-Transport-Security: max-age=31536000
/// 4. Test HTTP redirect: curl -I http://yourapp.com/
/// 5. Verify 301/308 redirect to HTTPS
/// 
/// HSTS cannot be tested in development HTTP environment.
/// 
/// Content-Security-Policy testing:
/// 1. Verify CSP header in browser DevTools → Network → Response Headers
/// 2. Check for violations in Console tab
/// 3. Validate Blazor WASM resources are allowed
/// </summary>
public class ProductionSecurityHeadersManualTests
{
    // This class documents manual testing procedures for production-only security headers
}
