using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ContosoShop.Shared.Models;

/// <summary>
/// Represents a customer purchase transaction with lifecycle tracking.
/// </summary>
public class Order
{
    /// <summary>
    /// Unique order identifier (displayed as order number)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Owner of this order
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// When order was placed (UTC)
    /// </summary>
    [Required]
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Current order state
    /// </summary>
    [Required]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Total order value in USD
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// When order was shipped (UTC), null if not yet shipped
    /// </summary>
    public DateTime? ShipDate { get; set; }

    /// <summary>
    /// When order was delivered (UTC), null if not yet delivered
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Navigation property: User who owns this order
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public User User { get; set; } = null!;

    /// <summary>
    /// Navigation property: Items in this order
    /// </summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
