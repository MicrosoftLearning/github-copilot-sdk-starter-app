using System.ComponentModel.DataAnnotations;

namespace ContosoShop.Shared.DTOs;

/// <summary>
/// Request model for returning specific items from an order
/// </summary>
public class ReturnItemsRequest
{
    /// <summary>
    /// List of items to return with quantities
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one item must be selected for return")]
    public List<ReturnItem> Items { get; set; } = new();
}

/// <summary>
/// Individual item to return
/// </summary>
public class ReturnItem
{
    /// <summary>
    /// Order item ID
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid item ID")]
    public int OrderItemId { get; set; }

    /// <summary>
    /// Quantity to return
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    /// <summary>
    /// Reason/justification for return
    /// </summary>
    [Required]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; } = "Customer has chosen to return item";
}
