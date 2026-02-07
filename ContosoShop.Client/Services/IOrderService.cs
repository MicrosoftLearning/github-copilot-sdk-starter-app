using ContosoShop.Shared.DTOs;
using ContosoShop.Shared.Models;

namespace ContosoShop.Client.Services;

/// <summary>
/// Service interface for order-related operations
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Retrieves all orders for the current user
    /// </summary>
    /// <returns>A list of orders sorted by date descending</returns>
    Task<List<Order>> GetOrdersAsync();

    /// <summary>
    /// Retrieves a specific order by ID
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>The order details, or null if not found</returns>
    Task<Order?> GetOrderByIdAsync(int id);

    /// <summary>
    /// Initiates a return for specific items in an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="items">List of items to return with quantities</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> ReturnOrderItemsAsync(int orderId, List<ReturnItem> items);
}
