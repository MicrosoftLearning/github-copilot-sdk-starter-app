namespace ContosoShop.Shared.DTOs;

/// <summary>
/// Inventory summary for a product
/// </summary>
public class InventorySummary
{
    public int ProductId { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Weight { get; set; }
    public string Dimensions { get; set; } = string.Empty;
    public int TotalInventory { get; set; }
    public int AvailableStock { get; set; }
    public int ReservedStock { get; set; }
    public int ReturnedItems { get; set; }
}
