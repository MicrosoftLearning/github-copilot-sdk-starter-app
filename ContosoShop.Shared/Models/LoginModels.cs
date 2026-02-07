using System.ComponentModel.DataAnnotations;

namespace ContosoShop.Shared.Models;

/// <summary>
/// Request model for user login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response model for login operation.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Indicates whether login was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if login failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// User name if login successful
    /// </summary>
    public string? UserName { get; set; }
}
