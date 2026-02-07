using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ContosoShop.Server.Data;
using Microsoft.AspNetCore.Identity;
using ContosoShop.Shared.Models;

namespace ContosoShop.Server.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration tests with in-memory database
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly object _lock = new object();
    private static bool _databaseSeeded = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ContosoContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using in-memory database for testing
            services.AddDbContext<ContosoContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Ensure database is created and seeded once
            var sp = services.BuildServiceProvider();
            lock (_lock)
            {
                if (!_databaseSeeded)
                {
                    using var scope = sp.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ContosoContext>();
                    var userManager = scopedServices.GetRequiredService<UserManager<User>>();

                    db.Database.EnsureCreated();
                    SeedTestData(db, userManager).Wait();
                    _databaseSeeded = true;
                }
            }
        });
    }

    private static async Task SeedTestData(ContosoContext context, UserManager<User> userManager)
    {
        // Only seed if not already seeded
        if (await context.Orders.AnyAsync())
        {
            return;
        }

        // Create test users with normalized email/username
        var john = new User
        {
            UserName = "john@contoso.com",
            Email = "john@contoso.com",
            NormalizedUserName = "JOHN@CONTOSO.COM",
            NormalizedEmail = "JOHN@CONTOSO.COM",
            Name = "John Doe",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            EmailConfirmed = true
        };

        var jane = new User
        {
            UserName = "jane@contoso.com",
            Email = "jane@contoso.com",
            NormalizedUserName = "JANE@CONTOSO.COM",
            NormalizedEmail = "JANE@CONTOSO.COM",
            Name = "Jane Smith",
            CreatedAt = DateTime.UtcNow.AddDays(-25),
            EmailConfirmed = true
        };

        await userManager.CreateAsync(john, "Password123!");
        await userManager.CreateAsync(jane, "Password123!");

        // Refetch users to get the assigned IDs
        john = await userManager.FindByEmailAsync("john@contoso.com");
        jane = await userManager.FindByEmailAsync("jane@contoso.com");

        // Create test orders
        var johnOrders = new[]
        {
            new Order { Id = 1, UserId = john!.Id, OrderDate = DateTime.UtcNow.AddDays(-10), Status = OrderStatus.Delivered, TotalAmount = 159.99m },
            new Order { Id = 2, UserId = john.Id, OrderDate = DateTime.UtcNow.AddDays(-5), Status = OrderStatus.Shipped, TotalAmount = 89.50m },
            new Order { Id = 3, UserId = john.Id, OrderDate = DateTime.UtcNow.AddDays(-2), Status = OrderStatus.Processing, TotalAmount = 234.75m },
        };

        var janeOrders = new[]
        {
            new Order { Id = 4, UserId = jane!.Id, OrderDate = DateTime.UtcNow.AddDays(-8), Status = OrderStatus.Delivered, TotalAmount = 45.00m },
            new Order { Id = 5, UserId = jane.Id, OrderDate = DateTime.UtcNow.AddDays(-3), Status = OrderStatus.Shipped, TotalAmount = 129.99m },
        };

        context.Orders.AddRange(johnOrders);
        context.Orders.AddRange(janeOrders);
        await context.SaveChangesAsync();
    }
}
