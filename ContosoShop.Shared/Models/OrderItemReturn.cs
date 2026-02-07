using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ContosoShop.Shared.Models;

/// <summary>
/// Represents an individual return transaction for order items.
/// Tracks each return event separately to support multiple returns with different justifications.
/// </summary>
public class OrderItemReturn
{
    /// <summary>
    /// Unique return transaction identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Order item being returned
    /// </summary>
    public int OrderItemId { get; set; }

    /// <summary>
    /// Quantity returned in this transaction
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// Reason/justification for this return
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// When this return was processed (UTC)
    /// </summary>
    [Required]
    public DateTime ReturnedDate { get; set; }

    /// <summary>
    /// Refund amount for this return transaction
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Navigation property: Parent order item
    /// </summary>
    [ForeignKey("OrderItemId")]
    [JsonIgnore]
    public OrderItem OrderItem { get; set; } = null!;
}
