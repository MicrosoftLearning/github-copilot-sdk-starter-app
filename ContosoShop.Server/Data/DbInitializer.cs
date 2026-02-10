using ContosoShop.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContosoShop.Server.Data;

/// <summary>
/// Initializes database with sample data for demo purposes with secure password hashing.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Seeds the database with demo users and sample orders using secure password hashing.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="userManager">User manager for password hashing</param>
    public static async Task InitializeAsync(ContosoContext context, UserManager<User> userManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (context.Orders.Any())
        {
            return; // DB has been seeded
        }

        // Seed Products catalog (25 product types)
        var products = await SeedProductsAsync(context);
        
        // Seed Inventory (100 items per product type)
        await SeedInventoryAsync(context, products);

        // Create demo users with hashed passwords (T034s)
        var mateo = new User
        {
            Name = "Mateo Gomez",
            Email = "mateo@contoso.com",
            UserName = "mateo@contoso.com",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(mateo, "Password123!");

        var megan = new User
        {
            Name = "Megan Bowen",
            Email = "megan@contoso.com",
            UserName = "megan@contoso.com",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(megan, "Password123!");

        // Get user IDs for order assignment (T036s)
        var mateoId = mateo.Id;
        var meganId = megan.Id;

        // Create sample orders with specific status distribution
        // Mateo gets 10 orders, Megan gets 10 orders (T035s)
        // Orders placed more than 9 days ago are Delivered or Returned
        // Mateo has 1 returned order, Megan has 0 returned orders
        var random = new Random(42); // Fixed seed for consistent data
        
        // Realistic price ranges for each product
        var productPrices = new Dictionary<string, (decimal min, decimal max)>
        {
            { "Wireless Mouse", (25m, 30m) },
            { "Keyboard", (45m, 60m) },
            { "Monitor", (180m, 250m) },
            { "HDMI Cable", (12m, 18m) },
            { "USB Cable", (8m, 12m) },
            { "Webcam", (65m, 90m) },
            { "USB Hub", (25m, 35m) },
            { "Headphones", (40m, 70m) },
            { "Mouse Pad", (10m, 15m) },
            { "Laptop Stand", (30m, 40m) },
            { "External SSD", (85m, 120m) },
            { "Phone Charger", (18m, 25m) },
            { "Desk Lamp", (35m, 50m) },
            { "Cable Organizer", (12m, 18m) },
            { "Laptop Bag", (45m, 65m) }
        };
        
        var productNames = productPrices.Keys.ToArray();
        
        // Create product name to ID lookup for linking OrderItems
        var productNameToId = productNames.ToDictionary(
            name => name,
            name => context.Products.First(p => p.Name == name).Id
        );
        
        var orders = new List<Order>();
        
        // Generate 20 orders total (10 per user)
        // Orders are distributed over the past 60 days
        // Orders older than 9 days: Delivered or Returned
        // Recent orders (last 9 days): Processing or Shipped
        
        var today = DateTime.UtcNow;
        
        // Mateo's 10 orders: IDs 1001-1010
        // - 7 Delivered (older than 9 days)
        // - 1 Returned (older than 9 days) - ID 1003
        // - 1 Shipped (within last 9 days)
        // - 1 Processing (within last 9 days)
        var mateoOrders = new[]
        {
            (1001, today.AddDays(-55), OrderStatus.Delivered),
            (1002, today.AddDays(-48), OrderStatus.Delivered),
            (1003, today.AddDays(-41), OrderStatus.Returned),  // Mateo's returned order
            (1004, today.AddDays(-35), OrderStatus.Delivered),
            (1005, today.AddDays(-28), OrderStatus.Delivered),
            (1006, today.AddDays(-21), OrderStatus.Delivered),
            (1007, today.AddDays(-14), OrderStatus.Delivered),
            (1008, today.AddDays(-10), OrderStatus.Delivered),
            (1009, today.AddDays(-6), OrderStatus.Shipped),
            (1010, today.AddDays(-2), OrderStatus.Processing)
        };
        
        // Megan's 10 orders: IDs 1011-1020
        // - 8 Delivered (older than 9 days)
        // - 0 Returned
        // - 1 Shipped (within last 9 days)
        // - 1 Processing (within last 9 days)
        var meganOrders = new[]
        {
            (1011, today.AddDays(-52), OrderStatus.Delivered),
            (1012, today.AddDays(-45), OrderStatus.Delivered),
            (1013, today.AddDays(-38), OrderStatus.Delivered),
            (1014, today.AddDays(-32), OrderStatus.Delivered),
            (1015, today.AddDays(-25), OrderStatus.Delivered),
            (1016, today.AddDays(-19), OrderStatus.Delivered),
            (1017, today.AddDays(-13), OrderStatus.Delivered),
            (1018, today.AddDays(-11), OrderStatus.Delivered),
            (1019, today.AddDays(-5), OrderStatus.Shipped),
            (1020, today.AddDays(-1), OrderStatus.Processing)
        };
        
        var allOrders = mateoOrders.Select(o => (o.Item1, o.Item2, o.Item3, mateoId))
            .Concat(meganOrders.Select(o => (o.Item1, o.Item2, o.Item3, meganId)))
            .ToArray();

        foreach (var (orderId, orderDate, status, userId) in allOrders)
        {
            // Determine number of items: most orders have 1-3 items
            int itemCount = random.Next(1, 4); // 1-3 items

            // Generate items
            var items = new List<OrderItem>();
            decimal totalAmount = 0;
            for (int j = 0; j < itemCount; j++)
            {
                var productIndex = random.Next(productNames.Length);
                var productName = productNames[productIndex];
                var productId = productNameToId[productName];
                var quantity = random.Next(1, 4); // 1-3 quantity per item
                
                // Use price from Product catalog
                var product = context.Products.First(p => p.Id == productId);
                var price = product.Price;
                
                items.Add(new OrderItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    Quantity = quantity,
                    Price = price
                });
                
                totalAmount += price * quantity;
            }

            // Create order with appropriate dates based on status
            var order = new Order
            {
                Id = orderId,
                UserId = userId,
                OrderDate = orderDate,
                Status = status,
                TotalAmount = totalAmount,
                Items = items
            };

            // Set ship/delivery dates based on status
            if (status == OrderStatus.Shipped || status == OrderStatus.Delivered || status == OrderStatus.Returned || status == OrderStatus.PartialReturn)
            {
                order.ShipDate = orderDate.AddDays(random.Next(1, 3));
            }

            if (status == OrderStatus.Delivered || status == OrderStatus.Returned || status == OrderStatus.PartialReturn)
            {
                order.DeliveryDate = orderDate.AddDays(random.Next(5, 10));
            }

            orders.Add(order);
        }

        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();

        // Reserve inventory for orders with Processing, Shipped, or Delivered status
        await ReserveInventoryForOrdersAsync(context, orders);

        Console.WriteLine($"Database initialized with {orders.Count} orders for users: {mateo.Name} ({orders.Count(o => o.UserId == mateoId)} orders), {megan.Name} ({orders.Count(o => o.UserId == meganId)} orders)");
    }

    /// <summary>
    /// Reserves inventory for existing orders based on their status
    /// </summary>
    private static async Task ReserveInventoryForOrdersAsync(ContosoContext context, List<Order> orders)
    {
        int totalReserved = 0;

        foreach (var order in orders)
        {
            // Only reserve inventory for orders that are Processing, Shipped, or Delivered
            // Pending orders haven't been processed yet, Returned orders are already handled
            if (order.Status == OrderStatus.Processing || 
                order.Status == OrderStatus.Shipped || 
                order.Status == OrderStatus.Delivered)
            {
                foreach (var item in order.Items)
                {
                    if (item.ProductId.HasValue)
                    {
                        // Get available items (status = "In Stock")
                        var availableItems = await context.InventoryItems
                            .Where(i => i.ProductId == item.ProductId.Value && i.Status == "In Stock")
                            .OrderBy(i => i.CreatedDate)
                            .Take(item.Quantity)
                            .ToListAsync();

                        // Mark items as Reserved
                        foreach (var inventoryItem in availableItems)
                        {
                            inventoryItem.Status = "Reserved";
                            inventoryItem.LastStatusChange = DateTime.UtcNow;
                            totalReserved++;
                        }
                    }
                }
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Reserved {totalReserved} inventory items for existing orders");
    }

    /// <summary>
    /// Seeds the product catalog with 25 product types
    /// </summary>
    private static async Task<List<Product>> SeedProductsAsync(ContosoContext context)
    {
        var products = new List<Product>
        {
            new Product { ItemNumber = "ITM-001", Name = "Wireless Mouse", Price = 27.99m, Weight = 0.25m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-002", Name = "Keyboard", Price = 52.99m, Weight = 1.5m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-003", Name = "Monitor", Price = 215.99m, Weight = 12.5m, Dimensions = "Large" },
            new Product { ItemNumber = "ITM-004", Name = "HDMI Cable", Price = 14.99m, Weight = 0.3m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-005", Name = "USB Cable", Price = 9.99m, Weight = 0.2m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-006", Name = "Webcam", Price = 77.99m, Weight = 0.8m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-007", Name = "USB Hub", Price = 29.99m, Weight = 0.4m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-008", Name = "Headphones", Price = 54.99m, Weight = 0.6m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-009", Name = "Mouse Pad", Price = 12.99m, Weight = 0.3m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-010", Name = "Laptop Stand", Price = 34.99m, Weight = 2.0m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-011", Name = "External SSD", Price = 99.99m, Weight = 0.5m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-012", Name = "Phone Charger", Price = 21.99m, Weight = 0.3m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-013", Name = "Desk Lamp", Price = 42.99m, Weight = 1.8m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-014", Name = "Cable Organizer", Price = 14.99m, Weight = 0.2m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-015", Name = "Laptop Bag", Price = 54.99m, Weight = 1.2m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-016", Name = "Wireless Keyboard", Price = 64.99m, Weight = 1.3m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-017", Name = "Gaming Mouse", Price = 49.99m, Weight = 0.3m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-018", Name = "USB Microphone", Price = 89.99m, Weight = 1.5m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-019", Name = "Monitor Arm", Price = 129.99m, Weight = 5.0m, Dimensions = "Large" },
            new Product { ItemNumber = "ITM-020", Name = "Ethernet Cable", Price = 12.99m, Weight = 0.4m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-021", Name = "Laptop Cooling Pad", Price = 34.99m, Weight = 1.0m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-022", Name = "Wireless Charger", Price = 29.99m, Weight = 0.5m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-023", Name = "Bluetooth Speaker", Price = 44.99m, Weight = 0.8m, Dimensions = "Small" },
            new Product { ItemNumber = "ITM-024", Name = "Drawing Tablet", Price = 79.99m, Weight = 1.2m, Dimensions = "Medium" },
            new Product { ItemNumber = "ITM-025", Name = "Document Scanner", Price = 149.99m, Weight = 3.5m, Dimensions = "Medium" }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        
        Console.WriteLine($"Seeded {products.Count} products into catalog");
        return products;
    }

    /// <summary>
    /// Seeds inventory with 100 items per product type
    /// </summary>
    private static async Task SeedInventoryAsync(ContosoContext context, List<Product> products)
    {
        var inventoryItems = new List<InventoryItem>();
        var now = DateTime.UtcNow;

        foreach (var product in products)
        {
            for (int i = 1; i <= 100; i++)
            {
                var serialNumber = $"{product.ItemNumber}-{i:D4}"; // e.g., ITM-001-0001
                inventoryItems.Add(new InventoryItem
                {
                    ProductId = product.Id,
                    SerialNumber = serialNumber,
                    Status = "In Stock",
                    HasReturnHistory = false,
                    CreatedDate = now,
                    LastStatusChange = null
                });
            }
        }

        context.InventoryItems.AddRange(inventoryItems);
        await context.SaveChangesAsync();
        
        Console.WriteLine($"Seeded {inventoryItems.Count} inventory items ({products.Count} products × 100 items)");
    }
}
