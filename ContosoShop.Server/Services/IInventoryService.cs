using ContosoShop.Shared.Models;

namespace ContosoShop.Server.Services;

/// <summary>
/// Interface for inventory management operations
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Reserves inventory items for an order
    /// </summary>
    /// <param name="orderItems">Items to reserve</param>
    /// <returns>True if all items reserved successfully</returns>
    Task<bool> ReserveInventoryAsync(IEnumerable<OrderItem> orderItems);
    
    /// <summary>
    /// Returns items back to inventory (marks as In Stock with return history)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="quantity">Quantity to return</param>
    /// <returns>True if items returned successfully</returns>
    Task<bool> ReturnToInventoryAsync(int productId, int quantity);
    
    /// <summary>
    /// Gets available stock count for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Count of items with status "In Stock"</returns>
    Task<int> GetAvailableStockAsync(int productId);
}
