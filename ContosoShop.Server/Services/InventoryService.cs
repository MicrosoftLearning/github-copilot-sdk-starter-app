using ContosoShop.Server.Data;
using ContosoShop.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoShop.Server.Services;

/// <summary>
/// Service for managing inventory operations (reservations, returns, stock counts)
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly ContosoContext _context;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ContosoContext context, ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Reserves inventory items by marking them as "Reserved"
    /// </summary>
    public async Task<bool> ReserveInventoryAsync(IEnumerable<OrderItem> orderItems)
    {
        try
        {
            foreach (var orderItem in orderItems)
            {
                if (!orderItem.ProductId.HasValue)
                {
                    _logger.LogWarning($"OrderItem {orderItem.Id} has no ProductId, skipping inventory reservation");
                    continue;
                }

                // Get available items (status = "In Stock")
                var availableItems = await _context.InventoryItems
                    .Where(i => i.ProductId == orderItem.ProductId.Value && i.Status == "In Stock")
                    .OrderBy(i => i.CreatedDate)
                    .Take(orderItem.Quantity)
                    .ToListAsync();

                if (availableItems.Count < orderItem.Quantity)
                {
                    _logger.LogWarning($"Insufficient inventory for Product {orderItem.ProductId}: requested {orderItem.Quantity}, available {availableItems.Count}");
                    return false;
                }

                // Mark items as Reserved
                foreach (var item in availableItems)
                {
                    item.Status = "Reserved";
                    item.LastStatusChange = DateTime.UtcNow;
                }

                _logger.LogInformation($"Reserved {availableItems.Count} items for Product {orderItem.ProductId}");
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving inventory");
            return false;
        }
    }

    /// <summary>
    /// Returns items to inventory by changing status back to "In Stock" with return history flag
    /// </summary>
    public async Task<bool> ReturnToInventoryAsync(int productId, int quantity)
    {
        try
        {
            // Get reserved items that can be returned
            var reservedItems = await _context.InventoryItems
                .Where(i => i.ProductId == productId && i.Status == "Reserved")
                .OrderBy(i => i.LastStatusChange)
                .Take(quantity)
                .ToListAsync();

            if (reservedItems.Count < quantity)
            {
                _logger.LogWarning($"Insufficient reserved inventory to return for Product {productId}: requested {quantity}, available {reservedItems.Count}");
                return false;
            }

            // Mark items as back in stock with return history
            foreach (var item in reservedItems)
            {
                item.Status = "In Stock";
                item.HasReturnHistory = true;
                item.LastStatusChange = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Returned {reservedItems.Count} items to inventory for Product {productId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning inventory");
            return false;
        }
    }

    /// <summary>
    /// Gets count of available inventory items
    /// </summary>
    public async Task<int> GetAvailableStockAsync(int productId)
    {
        return await _context.InventoryItems
            .Where(i => i.ProductId == productId && i.Status == "In Stock")
            .CountAsync();
    }
}
