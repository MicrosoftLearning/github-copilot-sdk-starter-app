using System.ComponentModel.DataAnnotations;

namespace ContosoShop.Shared.Models;

/// <summary>
/// Represents an individual physical item in inventory with serial number tracking
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }
    
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string SerialNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "In Stock"; // In Stock, Reserved, Sold, Returned
    
    public bool HasReturnHistory { get; set; } = false;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastStatusChange { get; set; }
    
    // Navigation property
    public Product Product { get; set; } = null!;
}
