using ContosoShop.Server.Data;
using ContosoShop.Shared.DTOs;
using ContosoShop.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoShop.Server.Services;

/// <summary>
/// Implementation of order business logic operations
/// </summary>
public class OrderService : IOrderService
{
    private readonly ContosoContext _context;
    private readonly IEmailService _emailService;
    private readonly IInventoryService _inventoryService;

    /// <summary>
    /// Initializes a new instance of the OrderService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="emailService">Email service for notifications</param>
    /// <param name="inventoryService">Inventory service for stock management</param>
    public OrderService(ContosoContext context, IEmailService emailService, IInventoryService inventoryService)
    {
        _context = context;
        _emailService = emailService;
        _inventoryService = inventoryService;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessItemReturnAsync(int orderId, List<ReturnItem> returnItems)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return false;
        }

        // Validate order is eligible for return (must be Delivered or partially Returned)
        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Returned)
        {
            return false;
        }

        // Process each item return
        foreach (var returnItem in returnItems)
        {
            var orderItem = order.Items.FirstOrDefault(i => i.Id == returnItem.OrderItemId);
            
            if (orderItem == null)
            {
                return false; // Item not found in order
            }

            // Validate quantity
            if (returnItem.Quantity <= 0 || returnItem.Quantity > (orderItem.Quantity - orderItem.ReturnedQuantity))
            {
                return false; // Invalid return quantity
            }

            // Calculate refund for this return transaction
            var itemRefundAmount = orderItem.Price * returnItem.Quantity;

            // Create return transaction record
            var returnTransaction = new OrderItemReturn
            {
                OrderItemId = orderItem.Id,
                Quantity = returnItem.Quantity,
                Reason = returnItem.Reason,
                ReturnedDate = DateTime.UtcNow,
                RefundAmount = itemRefundAmount
            };

            _context.OrderItemReturns.Add(returnTransaction);

            // Update returned quantity
            orderItem.ReturnedQuantity += returnItem.Quantity;

            // Return inventory to stock if ProductId exists
            if (orderItem.ProductId.HasValue)
            {
                await _inventoryService.ReturnToInventoryAsync(orderItem.ProductId.Value, returnItem.Quantity);
            }
        }

        // Calculate new order status based on returned items
        order.Status = CalculateOrderStatus(order.Items);

        await _context.SaveChangesAsync();

        // Calculate total refund amount
        decimal totalRefundAmount = returnItems.Sum(ri =>
        {
            var item = order.Items.First(i => i.Id == ri.OrderItemId);
            return item.Price * ri.Quantity;
        });

        // Send refund confirmation email (logged to console in dev)
        await _emailService.SendEmailAsync(
            "customer@example.com",
            "Return Confirmation",
            $"Your return for Order #{orderId} has been processed. Refund of {totalRefundAmount:C} will be credited to your original payment method within 3 business days following our receipt of the item(s).");

        return true;
    }

    /// <summary>
    /// Calculates the order status based on returned items
    /// </summary>
    private OrderStatus CalculateOrderStatus(ICollection<OrderItem> items)
    {
        var allItemsFullyReturned = items.All(i => i.IsFullyReturned);
        var anyItemReturned = items.Any(i => i.ReturnedQuantity > 0);

        if (allItemsFullyReturned)
        {
            return OrderStatus.Returned;
        }
        else if (anyItemReturned)
        {
            // Still Returned status, but with partial returns
            return OrderStatus.Returned;
        }
        else
        {
            return OrderStatus.Delivered;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReserveInventoryForOrderAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return false;
        }

        // Only reserve inventory for orders in Processing status
        if (order.Status != OrderStatus.Processing)
        {
            return false;
        }

        // Reserve inventory for all items in the order
        var result = await _inventoryService.ReserveInventoryAsync(order.Items);
        
        if (result)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }
}
