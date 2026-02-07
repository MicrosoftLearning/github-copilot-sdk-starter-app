# Research: ContosoShop E-commerce Support Portal

**Feature**: 001-ecommerce-support-portal  
**Date**: 2026-02-04  
**Phase**: 0 - Research & Best Practices

## Overview

This document captures research findings and decisions for implementing the ContosoShop E-commerce Support Portal. Since all technical context was specified without NEEDS CLARIFICATION markers, this research focuses on best practices, patterns, and implementation approaches for the chosen technology stack.

## Research Areas

### 1. Blazor WebAssembly Architecture Patterns

**Decision**: Use Blazor WebAssembly Hosted model with separate Client, Server, and Shared projects

**Rationale**:
- Blazor WASM Hosted provides clear separation between frontend (Client) and backend (Server)
- Shared project enables type-safe contracts between client and server
- Supports independent deployment strategy (Client to static hosting, Server to App Service)
- Built-in support for development with `dotnet run` serving both projects
- Aligns with Constitutional Principle V (API-Driven Development)

**Alternatives Considered**:
- **Blazor Server**: Rejected because it requires persistent connection and doesn't support offline scenarios or CDN deployment
- **Standalone Blazor WASM**: Rejected because it complicates API integration and type sharing
- **Separate repositories**: Rejected for MVP to maintain simplicity; acceptable future evolution

**Best Practices Applied**:
- Use HttpClient with base address configuration for API calls
- Implement service interfaces (IOrderService) for testability
- Use `GetFromJsonAsync<T>` and `PostAsJsonAsync<T>` for type-safe serialization
- Handle loading states with `@if (orders == null)` patterns
- Display error messages using Blazor's error boundary or conditional rendering

---

### 2. Entity Framework Core with SQLite for Development

**Decision**: Use EF Core with SQLite provider for local development, design for Azure SQL migration

**Rationale**:
- SQLite requires zero setup and provides file-based portability
- EF Core abstractions enable provider switching (UseSqlite → UseSqlServer) with minimal changes
- Code-first migrations work identically across providers
- Aligns with Constitutional Principle IV (Cloud-Ready Design)

**Alternatives Considered**:
- **SQL Server LocalDB**: Rejected due to Windows-only limitation, breaks cross-platform development
- **In-memory database**: Rejected because it doesn't persist between runs, complicates demos
- **PostgreSQL**: Rejected as overkill for MVP and adds external dependency

**Best Practices Applied**:
- Store connection string in `appsettings.json`: `"Data Source=App_Data/ContosoShop.db"`
- Use `Database.EnsureCreated()` or migrations for schema management
- Implement DbInitializer for seeding sample data
- Configure relationships using Fluent API in OnModelCreating
- Use async methods (`ToListAsync`, `FirstOrDefaultAsync`) for all database operations

**Migration Path to Azure SQL**:
```csharp
// Development (appsettings.Development.json)
"ConnectionStrings": {
  "DefaultConnection": "Data Source=App_Data/ContosoShop.db"
}

// Production (Azure App Service configuration)
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:contoso.database.windows.net,1433;Database=ContosoShop;..."
}

// Program.cs - single line change
// builder.Services.AddDbContext<ContosoContext>(options =>
//     options.UseSqlite(connectionString));  // Development
builder.Services.AddDbContext<ContosoContext>(options =>
    options.UseSqlServer(connectionString));  // Production
```

---

### 3. Dependency Injection for Testability

**Decision**: Use ASP.NET Core built-in DI with interface-based service registration

**Rationale**:
- Built-in DI is performant and well-integrated with ASP.NET Core lifecycle
- Interface abstraction enables mock implementations for testing
- Supports swapping implementations (EmailServiceDev → EmailServiceSendGrid)
- Aligns with Constitutional Principle II (Testable Architecture)

**Alternatives Considered**:
- **Third-party DI containers** (Autofac, StructureMap): Rejected as unnecessary complexity for MVP
- **Service Locator pattern**: Rejected as anti-pattern that hides dependencies
- **Manual instantiation**: Rejected because it prevents testing and violates DI principle

**Best Practices Applied**:
```csharp
// Interface definition
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

// Development implementation
public class EmailServiceDev : IEmailService
{
    private readonly ILogger<EmailServiceDev> _logger;
    
    public EmailServiceDev(ILogger<EmailServiceDev> logger)
    {
        _logger = logger;
    }
    
    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("EMAIL: To={To}, Subject={Subject}, Body={Body}", to, subject, body);
        return Task.CompletedTask;
    }
}

// Registration in Program.cs
builder.Services.AddScoped<IEmailService, EmailServiceDev>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddDbContext<ContosoContext>();
```

---

### 4. REST API Design Patterns

**Decision**: Use RESTful conventions with resource-based URLs and standard HTTP methods

**Rationale**:
- REST is industry standard for web APIs
- Clear mapping between URLs and resources (orders, order items)
- Standard HTTP methods convey intent (GET retrieve, POST create/action)
- Aligns with Constitutional Principle V (API-Driven Development)

**API Endpoints Designed**:

| Method | Endpoint | Purpose | Success | Error |
|--------|----------|---------|---------|-------|
| GET | `/api/orders` | List all orders for user | 200 + Order[] | 500 |
| GET | `/api/orders/{id}` | Get order details | 200 + Order | 404, 500 |
| POST | `/api/orders/{id}/return` | Initiate return | 200/204 | 400, 404, 500 |
| GET | `/api/support` | Get support info | 200 + SupportInfo | 500 |

**Alternatives Considered**:
- **GraphQL**: Rejected as overkill for simple CRUD operations
- **gRPC**: Rejected because Blazor WASM doesn't support gRPC-Web natively
- **RPC-style endpoints** (`/api/GetOrders`): Rejected as non-RESTful

**Best Practices Applied**:
- Use `[ApiController]` attribute for automatic model validation
- Return `ActionResult<T>` for strongly-typed responses
- Use `[HttpGet]`, `[HttpPost]` attributes with route templates
- Return appropriate status codes (200, 400, 404, 500)
- Use ProblemDetails for standardized error responses
- Add XML documentation comments for API documentation

**Example Controller**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        var orders = await _orderService.GetOrdersForUserAsync(GetCurrentUserId());
        return Ok(orders);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id, GetCurrentUserId());
        if (order == null)
            return NotFound();
        return Ok(order);
    }
    
    [HttpPost("{id}/return")]
    public async Task<ActionResult> ReturnOrder(int id)
    {
        var result = await _orderService.ProcessReturnAsync(id, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result.ErrorMessage);
        return NoContent();
    }
}
```

---

### 5. Error Handling and User Feedback

**Decision**: Use try-catch at controller level with user-friendly error messages, Blazor error boundaries for UI

**Rationale**:
- Controller-level exception handling prevents leaking implementation details
- User-friendly messages improve UX (Constitutional requirement FR-011)
- Blazor error boundaries catch unhandled exceptions in UI components
- Logging provides debugging information without exposing to users

**Alternatives Considered**:
- **Global exception middleware**: Considered but controller-level is sufficient for MVP
- **Result pattern everywhere**: Rejected as over-engineering for simple app
- **Throwing custom exceptions**: Acceptable but return error models is simpler

**Best Practices Applied**:
```csharp
// Controller error handling
[HttpPost("{id}/return")]
public async Task<ActionResult> ReturnOrder(int id)
{
    try
    {
        var result = await _orderService.ProcessReturnAsync(id, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing return for order {OrderId}", id);
        return StatusCode(500, new { message = "Unable to process return. Please try again later." });
    }
}

// Blazor error handling
@code {
    private string? errorMessage;
    
    private async Task HandleReturnOrder()
    {
        try
        {
            errorMessage = null;
            var response = await OrderService.ReturnOrderAsync(orderId);
            // Success handling
        }
        catch (HttpRequestException ex)
        {
            errorMessage = "Unable to process return. Please check your connection and try again.";
            _logger.LogError(ex, "Failed to return order {OrderId}", orderId);
        }
    }
}
```

---

### 6. Database Seeding Strategy

**Decision**: Seed data in DbInitializer called from Program.cs, check for existing data before seeding

**Rationale**:
- Automatic seeding ensures demo works out-of-the-box
- Checking for existing data prevents duplicates on restart
- Seeding in separate class keeps Program.cs clean
- Aligns with Functional Requirement FR-019

**Alternatives Considered**:
- **Migration seed data**: Rejected because it complicates migrations and can't be conditional
- **Manual SQL scripts**: Rejected because it breaks EF Core model and requires manual execution
- **Always re-seed**: Rejected because it causes primary key conflicts

**Best Practices Applied**:
```csharp
public static class DbInitializer
{
    public static void Initialize(ContosoContext context)
    {
        context.Database.EnsureCreated();
        
        // Check if data already exists
        if (context.Orders.Any())
            return;  // DB has been seeded
        
        // Create demo user
        var user = new User { Id = 1, Name = "John Doe", Email = "john.doe@example.com" };
        
        // Create sample orders
        var orders = new[]
        {
            new Order
            {
                Id = 1001,
                UserId = 1,
                OrderDate = DateTime.Now.AddDays(-30),
                Status = OrderStatus.Delivered,
                DeliveryDate = DateTime.Now.AddDays(-23),
                TotalAmount = 59.99m,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductName = "Wireless Mouse", Quantity = 1, Price = 25.00m },
                    new OrderItem { ProductName = "Keyboard", Quantity = 1, Price = 34.99m }
                }
            },
            // ... more orders
        };
        
        context.Orders.AddRange(orders);
        context.SaveChanges();
    }
}

// Program.cs
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ContosoContext>();
    DbInitializer.Initialize(context);
}
```

---

### 7. Configuration Management for Cloud-Ready Architecture

**Decision**: Use appsettings.json hierarchy with environment-specific overrides

**Rationale**:
- ASP.NET Core configuration system supports multiple sources
- Environment-specific files override base settings
- Azure App Service can inject configuration via environment variables
- Aligns with Constitutional Principle IV (Cloud-Ready Design)

**Configuration Hierarchy**:
1. `appsettings.json` (base configuration)
2. `appsettings.Development.json` (development overrides)
3. Environment variables (production overrides from Azure)
4. User secrets (local development secrets)

**Best Practices Applied**:
```json
// appsettings.json (checked into source control)
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/ContosoShop.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Features": {
    "EnableSwagger": false
  }
}

// appsettings.Development.json (checked into source control)
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "Features": {
    "EnableSwagger": true
  }
}

// Azure App Service Configuration (not in source control)
ConnectionStrings__DefaultConnection=Server=tcp:...
Features__EnableSwagger=false
```

---

## Technology Stack Validation

All technology choices align with Constitutional Principles and Success Criteria:

| Technology | Constitutional Alignment | Success Criteria Alignment |
|------------|-------------------------|---------------------------|
| .NET 8 | Latest framework with security updates (Principle I) | SC-004: Single SDK dependency |
| Blazor WASM | API-driven architecture (Principle V) | SC-006: Responsive UI 320px-1920px |
| EF Core | Testable via interfaces (Principle II), Cloud-ready (Principle IV) | SC-010: Provider switching support |
| SQLite | Local dev friendly (Principle IV) | SC-004: Zero external dependencies |
| ASP.NET Core | Security defaults, DI built-in (Principles I, II) | SC-007: <500ms API response |
| Bootstrap 5 | Responsive design | SC-006: Mobile/desktop support |
| xUnit | Standard .NET testing | Principle II: Integration tests |

---

## Implementation Readiness

✅ **All decisions finalized** - No NEEDS CLARIFICATION markers remain  
✅ **Best practices documented** - Ready for implementation phase  
✅ **Patterns selected** - Consistent approach across all layers  
✅ **Constitutional compliance** - All principles satisfied  
✅ **Migration path clear** - SQLite → Azure SQL documented

**Next Phase**: Proceed to Phase 1 (Design) to create data-model.md, contracts/, and quickstart.md
