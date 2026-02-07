using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ContosoShop.Shared.Models;

/// <summary>
/// Represents an individual line item within an order.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Unique item identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Parent order
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Product reference (nullable for backward compatibility with existing data)
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Name of purchased product
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Number of units purchased
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// Unit price in USD at time of purchase
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>
    /// Number of units returned
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int ReturnedQuantity { get; set; } = 0;

    /// <summary>
    /// Navigation property: Parent order
    /// </summary>
    [ForeignKey("OrderId")]
    [JsonIgnore]
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property: Product reference
    /// </summary>
    [ForeignKey("ProductId")]
    [JsonIgnore]
    public Product? Product { get; set; }

    /// <summary>
    /// Navigation property: Return transactions for this item
    /// </summary>
    [JsonIgnore]
    public ICollection<OrderItemReturn> Returns { get; set; } = new List<OrderItemReturn>();

    /// <summary>
    /// Calculated property: Item subtotal (not stored in DB)
    /// </summary>
    [NotMapped]
    public decimal Subtotal => Quantity * Price;

    /// <summary>
    /// Calculated property: Whether item is fully returned
    /// </summary>
    [NotMapped]
    public bool IsFullyReturned => ReturnedQuantity >= Quantity;

    /// <summary>
    /// Calculated property: Whether item is partially returned
    /// </summary>
    [NotMapped]
    public bool IsPartiallyReturned => ReturnedQuantity > 0 && ReturnedQuantity < Quantity;

    /// <summary>
    /// Calculated property: Remaining quantity not returned
    /// </summary>
    [NotMapped]
    public int RemainingQuantity => Math.Max(0, Quantity - ReturnedQuantity);
}
