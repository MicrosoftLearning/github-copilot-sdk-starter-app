using System.ComponentModel.DataAnnotations;

namespace ContosoShop.Shared.Models;

/// <summary>
/// Represents a product in the catalog
/// </summary>
public class Product
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ItemNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public decimal Price { get; set; }
    
    [Required]
    public decimal Weight { get; set; } // in pounds
    
    [Required]
    [MaxLength(20)]
    public string Dimensions { get; set; } = "Medium"; // Small, Medium, Large
    
    // Navigation property
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
