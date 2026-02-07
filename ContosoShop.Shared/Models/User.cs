using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace ContosoShop.Shared.Models;

/// <summary>
/// Represents a customer account in the system with authentication support.
/// Extends IdentityUser for ASP.NET Core Identity integration.
/// </summary>
public class User : IdentityUser<int>
{
    /// <summary>
    /// Customer full name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the user account was created (for audit trails)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: Orders belonging to this user
    /// </summary>
    [JsonIgnore]
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
