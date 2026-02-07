using ContosoShop.Shared.DTOs;

namespace ContosoShop.Server.Services;

/// <summary>
/// Service interface for order business logic operations
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Processes a return request for specific items in an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="returnItems">List of items to return with quantities</param>
    /// <returns>True if return was processed successfully, false if validation failed</returns>
    Task<bool> ProcessItemReturnAsync(int orderId, List<ReturnItem> returnItems);

    /// <summary>
    /// Reserves inventory for an order (called when order status is Processing)
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <returns>True if inventory was reserved successfully</returns>
    Task<bool> ReserveInventoryForOrderAsync(int orderId);
}
