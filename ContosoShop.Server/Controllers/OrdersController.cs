using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Antiforgery;
using ContosoShop.Server.Data;
using ContosoShop.Server.Services;
using ContosoShop.Shared.DTOs;
using ContosoShop.Shared.Models;
using System.Security.Claims;

namespace ContosoShop.Server.Controllers;

/// <summary>
/// API controller for order management operations with authentication and authorization
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // T059s - Require authentication for all endpoints
public class OrdersController : ControllerBase
{
    private readonly ContosoContext _context;
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;
    private readonly IAntiforgery _antiforgery;

    /// <summary>
    /// Initializes a new instance of the OrdersController
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="orderService">Order business logic service</param>
    /// <param name="logger">Logger for audit trails</param>
    /// <param name="antiforgery">Antiforgery service for CSRF protection</param>
    public OrdersController(ContosoContext context, IOrderService orderService, ILogger<OrdersController> logger, IAntiforgery antiforgery)
    {
        _context = context;
        _orderService = orderService;
        _logger = logger;
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Gets all orders for the authenticated user, sorted by order date descending
    /// </summary>
    /// <returns>List of orders with related items belonging to the authenticated user</returns>
    /// <response code="200">Returns the list of orders for authenticated user</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<Order>>> GetOrders()
    {
        // T060s - Extract userId from authenticated claims
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        // T061s - Filter orders by authenticated user
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        // T063s - Log order access (NO financial amounts)
        _logger.LogInformation(
            "Order list accessed by UserId: {UserId}, OrderCount: {OrderCount}, Timestamp: {Timestamp}",
            userId,
            orders.Count,
            DateTime.UtcNow);

        return Ok(orders);
    }

    /// <summary>
    /// Gets a specific order by ID with all related items (authorized - must own the order)
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>Order details including items, dates, and status</returns>
    /// <response code="200">Returns the order details</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">Order belongs to different user</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        // T073s - Extract userId from authenticated claims
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        // Find order and include items
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        // T075s - Return 404 if order doesn't exist (don't leak existence to unauthorized users)
        if (order == null)
        {
            return NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Not Found",
                status = 404,
                detail = $"Order with ID {id} not found"
            });
        }

        // T074s - Verify ownership: if order.UserId != userId, return 403 Forbidden
        if (order.UserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized access attempt: UserId {UserId} attempted to access Order {OrderId} belonging to UserId {OwnerId}",
                userId,
                id,
                order.UserId);

            return Forbid();
        }

        // T076s - Log order details access
        _logger.LogInformation(
            "Order details accessed: UserId {UserId}, OrderId {OrderId}, Timestamp {Timestamp}",
            userId,
            id,
            DateTime.UtcNow);

        return Ok(order);
    }

    /// <summary>
    /// Initiates a return for a delivered order (CSRF protected)
    /// </summary>
    /// <param name="id">The order ID to return</param>
    /// <returns>No content if successful, BadRequest if order is not eligible</returns>
    /// <response code="204">Return processed successfully</response>
    /// <response code="400">Order is not eligible for return</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">Order belongs to different user</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/return-items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ReturnOrderItems(int id, [FromBody] ReturnItemsRequest request)
    {
        // T085s - Validate CSRF token manually for API endpoint
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Invalid or missing anti-forgery token."
            });
        }

        // Validate request
        if (request == null || request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "At least one item must be selected for return."
            });
        }

        // T086s - Extract userId from authenticated claims
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        // Find order
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        // T087s - Verify ownership
        if (order.UserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized return attempt: UserId {UserId} attempted to return items from Order {OrderId} belonging to UserId {OwnerId}",
                userId,
                id,
                order.UserId);

            return Forbid();
        }

        // T088s - Validate order status is Delivered or Returned (for partial returns)
        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Returned)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Order is not eligible for return. Only delivered orders can be returned."
            });
        }

        // Validate all items belong to this order and have valid quantities
        foreach (var returnItem in request.Items)
        {
            var orderItem = order.Items.FirstOrDefault(i => i.Id == returnItem.OrderItemId);
            if (orderItem == null)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Bad Request",
                    status = 400,
                    detail = $"Item {returnItem.OrderItemId} not found in order."
                });
            }

            var availableQuantity = orderItem.Quantity - orderItem.ReturnedQuantity;
            if (returnItem.Quantity <= 0 || returnItem.Quantity > availableQuantity)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Bad Request",
                    status = 400,
                    detail = $"Invalid return quantity for {orderItem.ProductName}. Available: {availableQuantity}"
                });
            }
        }

        // Process the return
        var success = await _orderService.ProcessItemReturnAsync(id, request.Items);

        if (!success)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Failed to process return."
            });
        }

        // T089s - Log return action (NO PII, NO financial amounts)
        _logger.LogInformation(
            "Item return processed: UserId {UserId}, OrderId {OrderId}, ItemCount {ItemCount}, Timestamp {Timestamp}",
            userId,
            id,
            request.Items.Count,
            DateTime.UtcNow);

        return NoContent();
    }
}
