using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ContosoShop.Shared.Models;

namespace ContosoShop.Server.Controllers;

/// <summary>
/// Handles authentication operations including login and logout.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and creates a session.
    /// Rate limited to 5 failures per 15 minutes per IP.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Login result with success status</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get IP address for logging (T042s)
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Log authentication failure with masked email (T044s)
            _logger.LogWarning(
                "Authentication failed for email: {Email}, IP: {IpAddress}, Timestamp: {Timestamp}",
                request.Email, // Will be masked by PiiSanitizingLogger
                ipAddress,
                DateTime.UtcNow);

            return Unauthorized(new LoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password"
            });
        }

        // Attempt sign in
        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Log authentication success (T043s)
            _logger.LogInformation(
                "Authentication succeeded for user: {UserId}, IP: {IpAddress}, Timestamp: {Timestamp}",
                user.Id,
                ipAddress,
                DateTime.UtcNow);

            return Ok(new LoginResponse
            {
                Success = true,
                UserName = user.Name
            });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning(
                "Account locked out for email: {Email}, IP: {IpAddress}, Timestamp: {Timestamp}",
                request.Email,
                ipAddress,
                DateTime.UtcNow);

            return Unauthorized(new LoginResponse
            {
                Success = false,
                ErrorMessage = "Account locked due to too many failed attempts. Please try again in 15 minutes."
            });
        }

        // Log authentication failure (T044s)
        _logger.LogWarning(
            "Authentication failed for email: {Email}, IP: {IpAddress}, Timestamp: {Timestamp}",
            request.Email,
            ipAddress,
            DateTime.UtcNow);

        return Unauthorized(new LoginResponse
        {
            Success = false,
            ErrorMessage = "Invalid email or password"
        });
    }

    /// <summary>
    /// Logs out the current user and terminates their session.
    /// </summary>
    /// <returns>No content on success</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        _logger.LogInformation(
            "User logged out, Timestamp: {Timestamp}",
            DateTime.UtcNow);

        return NoContent();
    }

    /// <summary>
    /// Gets the current user's authentication status and basic info.
    /// Used by the client to check if user is authenticated.
    /// </summary>
    /// <returns>User authentication information</returns>
    [HttpGet("user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var email = User.Identity.Name;
            var user = email != null ? await _userManager.FindByEmailAsync(email) : null;

            return Ok(new
            {
                isAuthenticated = true,
                email = email,
                name = user?.Name
            });
        }

        return Ok(new
        {
            isAuthenticated = false,
            email = (string?)null,
            name = (string?)null
        });
    }

    /// <summary>
    /// Gets the current CSRF token for form submissions.
    /// </summary>
    /// <returns>CSRF token</returns>
    [HttpGet("csrf-token")]
    [Authorize]
    public IActionResult GetCsrfToken()
    {
        var tokens = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
        var tokenSet = tokens.GetAndStoreTokens(HttpContext);
        
        return Ok(new { token = tokenSet.RequestToken });
    }
}
