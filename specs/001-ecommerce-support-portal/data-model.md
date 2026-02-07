# Data Model: ContosoShop E-commerce Support Portal

**Feature**: 001-ecommerce-support-portal  
**Date**: 2026-02-04  
**Phase**: 1 - Design

## Overview

This document defines the data model for the ContosoShop E-commerce Support Portal. The model supports viewing order history, order details, initiating returns, and tracking order status through the order lifecycle.

## Entity Relationship Diagram

```
┌─────────────┐       ┌──────────────────┐       ┌─────────────────┐
│    User     │ 1   * │      Order       │ 1   * │   OrderItem     │
├─────────────┤───────├──────────────────┤───────├─────────────────┤
│ Id (PK)     │       │ Id (PK)          │       │ Id (PK)         │
│ Name        │       │ UserId (FK)      │       │ OrderId (FK)    │
│ Email       │       │ OrderDate        │       │ ProductName     │
└─────────────┘       │ Status           │       │ Quantity        │
                      │ TotalAmount      │       │ Price           │
                      │ ShipDate?        │       └─────────────────┘
                      │ DeliveryDate?    │
                      └──────────────────┘
                              │
                              │ uses
                              ▼
                      ┌──────────────┐
                      │ OrderStatus  │
                      │  (Enum)      │
                      ├──────────────┤
                      │ Processing   │
                      │ Shipped      │
                      │ Delivered    │
                      │ Returned     │
                      └──────────────┘
```

## Entities

### User

Represents a customer account in the system.

**Note**: For MVP, using simplified single-user model ("John Doe" demo user). Architecture supports multi-user expansion.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | PK, Identity | Unique user identifier |
| Name | string(100) | NOT NULL | Customer full name |
| Email | string(255) | NOT NULL, Unique | Customer email address |

**Relationships**:
- One-to-Many with Order (One user has many orders)

**Validation Rules**:
- Email must be valid format
- Name required, 1-100 characters

**Index Strategy**:
- Primary key on Id (clustered)
- Unique index on Email

---

### Order

Represents a customer purchase transaction with lifecycle tracking.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | PK, Identity | Unique order identifier (displayed as order number) |
| UserId | int | FK → User.Id, NOT NULL | Owner of this order |
| OrderDate | DateTime | NOT NULL | When order was placed (UTC) |
| Status | OrderStatus (int) | NOT NULL | Current order state |
| TotalAmount | decimal(18,2) | NOT NULL, >= 0 | Total order value in USD |
| ShipDate | DateTime? | NULL | When order was shipped (UTC), null if not yet shipped |
| DeliveryDate | DateTime? | NULL | When order was delivered (UTC), null if not yet delivered |

**Relationships**:
- Many-to-One with User (Many orders belong to one user)
- One-to-Many with OrderItem (One order has many items)

**Validation Rules**:
- UserId must reference existing User
- OrderDate <= current date
- TotalAmount >= 0
- If Status = Shipped, ShipDate must be set
- If Status = Delivered, both ShipDate and DeliveryDate must be set
- ShipDate >= OrderDate (if set)
- DeliveryDate >= ShipDate (if set)
- Status transition rules (see State Transitions below)

**Business Rules**:
- Only orders with Status = Delivered can be returned
- Return operation changes Status to Returned
- Orders marked Returned cannot be returned again

**Index Strategy**:
- Primary key on Id (clustered)
- Index on UserId for efficient user order lookups
- Index on OrderDate for chronological sorting
- Composite index on (UserId, OrderDate DESC) for order history queries

---

### OrderItem

Represents an individual line item within an order.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | PK, Identity | Unique item identifier |
| OrderId | int | FK → Order.Id, NOT NULL | Parent order |
| ProductName | string(200) | NOT NULL | Name of purchased product |
| Quantity | int | NOT NULL, > 0 | Number of units purchased |
| Price | decimal(18,2) | NOT NULL, >= 0 | Unit price in USD at time of purchase |

**Relationships**:
- Many-to-One with Order (Many items belong to one order)

**Validation Rules**:
- OrderId must reference existing Order
- ProductName required, 1-200 characters
- Quantity must be >= 1
- Price must be >= 0
- Quantity × Price should contribute to Order.TotalAmount

**Calculation**:
- Item subtotal = Quantity × Price
- Order.TotalAmount = SUM(OrderItem.Quantity × OrderItem.Price) for all items in order

**Index Strategy**:
- Primary key on Id (clustered)
- Index on OrderId for efficient item lookup by order

**Note**: For MVP, product information is denormalized into OrderItem. No separate Product table exists. This simplifies demo while still supporting the core use cases.

---

### OrderStatus (Enumeration)

Represents the current state of an order in its lifecycle.

| Value | Int | Description | User-Visible Display |
|-------|-----|-------------|---------------------|
| Processing | 0 | Order received, preparing for shipment | "Processing" (blue) |
| Shipped | 1 | Order handed to carrier, in transit | "Shipped" (yellow/orange) |
| Delivered | 2 | Order delivered to customer | "Delivered" (green) |
| Returned | 3 | Customer initiated return, refund processed | "Returned" (gray) |

**State Transitions**:

```
   [Order Placed]
        │
        ▼
   Processing ──► Shipped ──► Delivered ──► Returned
                                   ▲           │
                                   └───────────┘
                                  (can return)
```

**Transition Rules**:
- Processing → Shipped: When order is packed and shipped (sets ShipDate)
- Shipped → Delivered: When carrier confirms delivery (sets DeliveryDate)
- Delivered → Returned: When customer initiates return (User Story 3)
- **Invalid transitions**: Cannot skip states, cannot return from Processing/Shipped

**Implementation**:
```csharp
public enum OrderStatus
{
    Processing = 0,
    Shipped = 1,
    Delivered = 2,
    Returned = 3
}
```

---

## Data Model Implementation

### EF Core Entity Classes

```csharp
// User.cs
public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

// Order.cs
public class Order
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    public DateTime OrderDate { get; set; }
    
    [Required]
    public OrderStatus Status { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }
    
    public DateTime? ShipDate { get; set; }
    
    public DateTime? DeliveryDate { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

// OrderItem.cs
public class OrderItem
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }
    
    // Navigation property
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;
    
    // Calculated property (not stored in DB)
    [NotMapped]
    public decimal Subtotal => Quantity * Price;
}

// OrderStatus.cs (enum)
public enum OrderStatus
{
    Processing = 0,
    Shipped = 1,
    Delivered = 2,
    Returned = 3
}
```

### DbContext Configuration

```csharp
public class ContosoContext : DbContext
{
    public ContosoContext(DbContextOptions<ContosoContext> options)
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
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
            
            // Relationship
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
            
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            // Relationship
            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

---

## Sample Data

### Demo User
- Id: 1
- Name: "John Doe"
- Email: "john.doe@example.com"

### Sample Orders

**Order 1001** (Delivered - returnable)
- User: John Doe (Id=1)
- Date: 30 days ago
- Status: Delivered
- Total: $59.99
- Items:
  - Wireless Mouse × 1 @ $25.00
  - Keyboard × 1 @ $34.99

**Order 1002** (Shipped - not returnable yet)
- User: John Doe (Id=1)
- Date: 5 days ago
- Status: Shipped
- Total: $15.00
- Items:
  - HDMI Cable × 1 @ $15.00

**Order 1003** (Processing - not returnable)
- User: John Doe (Id=1)
- Date: 1 day ago
- Status: Processing
- Total: $129.99
- Items:
  - Webcam × 1 @ $89.99
  - USB Hub × 2 @ $20.00

**Order 1004** (Returned - already returned)
- User: John Doe (Id=1)
- Date: 60 days ago
- Status: Returned
- Total: $45.00
- Items:
  - USB Cable × 3 @ $15.00

---

## Data Integrity

### Constraints
- Foreign keys enforce referential integrity (User ← Order ← OrderItem)
- Check constraints on Quantity > 0, Price >= 0, TotalAmount >= 0
- Unique constraint on User.Email

### Cascading Behavior
- Delete User: RESTRICT (cannot delete user with orders)
- Delete Order: CASCADE (deletes associated OrderItems)

### Transaction Boundaries
- Return operation: Update Order.Status within single transaction
- Order creation: Insert Order + OrderItems in single transaction

---

## Database Migration Strategy

### SQLite (Development)
- File-based: `App_Data/ContosoShop.db`
- Migrations apply on startup via `Database.EnsureCreated()` or manual migrations
- Data seeding on first run

### Azure SQL (Production)
- Server-based connection string from App Service configuration
- Migrations applied via CI/CD pipeline or `dotnet ef database update`
- No data seeding (production data managed separately)

### Migration Commands
```bash
# Create initial migration
dotnet ef migrations add InitialCreate --project ContosoShop.Server

# Apply migrations
dotnet ef database update --project ContosoShop.Server

# Generate SQL script
dotnet ef migrations script --project ContosoShop.Server --output schema.sql
```

---

## Alignment with Specifications

| Specification Requirement | Data Model Support |
|---------------------------|-------------------|
| FR-001: Display order list | Order entity with UserId index for efficient queries |
| FR-002: View order details | OrderItem relationship with cascade loading |
| FR-003: Order status values | OrderStatus enum with 4 defined states |
| FR-005: Order timeline | OrderDate, ShipDate, DeliveryDate fields |
| FR-006: Return delivered orders | Status field enables conditional return logic |
| FR-008: Update status to Returned | OrderStatus.Returned state in enum |
| FR-013: Persist order data | Full entity model with relationships |
| Key Entity: Order | Fully modeled with all required attributes |
| Key Entity: OrderItem | Fully modeled with relationship to Order |
| Key Entity: User | Simplified for demo, expandable for production |
| Key Entity: OrderStatus | Enum with lifecycle states |

---

## Next Steps

- [ ] Create EF Core migrations for schema
- [ ] Implement DbInitializer for data seeding
- [ ] Configure DbContext in Program.cs
- [ ] Create repository interfaces (optional) or use DbContext directly
- [ ] Validate data model supports all user stories
