using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ContosoShop.Shared.Models;

namespace ContosoShop.Server.Data;

/// <summary>
/// Entity Framework Core database context for ContosoShop application with Identity support.
/// Extends IdentityDbContext for ASP.NET Core Identity integration.
/// </summary>
public class ContosoContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public ContosoContext(DbContextOptions<ContosoContext> options)
        : base(options)
    {
    }

    // Note: Users DbSet is inherited from IdentityDbContext

    /// <summary>
    /// Orders placed by users
    /// </summary>
    public DbSet<Order> Orders { get; set; } = null!;

    /// <summary>
    /// Individual items within orders
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    /// <summary>
    /// Return transactions for order items
    /// </summary>
    public DbSet<OrderItemReturn> OrderItemReturns { get; set; } = null!;

    /// <summary>
    /// Product catalog
    /// </summary>
    public DbSet<Product> Products { get; set; } = null!;

    /// <summary>
    /// Physical inventory items with serial numbers
    /// </summary>
    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration (additional properties beyond IdentityUser)
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OrderDate);
            entity.HasIndex(e => new { e.UserId, e.OrderDate });

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.OrderDate).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            // Relationship: User -> Orders
            entity.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);

            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Relationship: Order -> OrderItems
            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Product -> OrderItems (optional for backward compatibility)
            entity.HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ItemNumber).IsUnique();
            entity.HasIndex(e => e.Name);

            entity.Property(e => e.ItemNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            entity.Property(e => e.Weight)
                .HasColumnType("decimal(10,2)")
                .IsRequired();
            entity.Property(e => e.Dimensions).IsRequired().HasMaxLength(20);
        });

        // InventoryItem configuration
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.SerialNumber).IsUnique();
            entity.HasIndex(e => new { e.ProductId, e.Status });

            entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedDate).IsRequired();

            // Relationship: Product -> InventoryItems
            entity.HasOne(ii => ii.Product)
                .WithMany(p => p.InventoryItems)
                .HasForeignKey(ii => ii.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
