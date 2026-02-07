using ContosoShop.Server.Data;
using ContosoShop.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoShop.Server.Controllers;

/// <summary>
/// API controller for inventory operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly ContosoContext _context;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(ContosoContext context, ILogger<InventoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets inventory summary for all products
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<InventorySummary>>> GetInventorySummary()
    {
        var inventorySummaries = await _context.Products
            .Select(p => new InventorySummary
            {
                ProductId = p.Id,
                ItemNumber = p.ItemNumber,
                Name = p.Name,
                Price = p.Price,
                Weight = p.Weight,
                Dimensions = p.Dimensions,
                TotalInventory = p.InventoryItems.Count,
                AvailableStock = p.InventoryItems.Count(i => i.Status == "In Stock"),
                ReservedStock = p.InventoryItems.Count(i => i.Status == "Reserved"),
                ReturnedItems = p.InventoryItems.Count(i => i.HasReturnHistory)
            })
            .OrderBy(p => p.ItemNumber)
            .ToListAsync();

        _logger.LogInformation($"Inventory summary accessed: {inventorySummaries.Count} products, Timestamp: {DateTime.UtcNow:MM/dd/yyyy HH:mm:ss}");

        return Ok(inventorySummaries);
    }
}
