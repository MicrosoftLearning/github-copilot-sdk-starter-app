# ContosoShop E-commerce Support Portal – Technical Architecture and Stack

This document provides a technical overview of how the application's features (described in AppFeatures.md) are implemented. We outline the architecture, frameworks, and key components, and highlight how the design facilitates local development as well as future cloud migration.

## 1. Solution Architecture Overview

The project follows a **Blazor WebAssembly Hosted architecture**, which means it is split into a client and a server:

- **ContosoShop.Client (Blazor WebAssembly):** This is the front-end running in the browser. It's a single-page application (SPA) written in C# and HTML (Razor components). It contains the UI logic, forms, and calls the backend API via HttpClient. It was created as a standalone Blazor WASM project and later configured to work with the API. (In our solution, it's a separate project that can be deployed independently of the server if needed.)

- **ContosoShop.Server (ASP.NET Core Web API):** This is the back-end REST API built with ASP.NET Core 8 (running on .NET 8). It exposes endpoints under /api/ that the client calls. It also hosts the Blazor client's static files when run in a combined mode (for simplicity in local dev, we actually serve the Blazor app from the same domain via the ASP.NET project, using the ASP.NET Core Hosted template setup). The server contains all business logic – like querying the database or updating an order – and MUST enforce authentication and authorization rules.

**SECURITY ARCHITECTURE:**
- **Authentication:** MUST implement ASP.NET Core Identity or JWT authentication
- **Authorization:** ALL API endpoints that access user data MUST use `[Authorize]` attribute
- **User Context:** Controllers MUST obtain the current user ID from `User.FindFirstValue(ClaimTypes.NameIdentifier)` or equivalent claims
- **CSRF Protection:** Anti-forgery tokens MUST be validated for all POST/PUT/DELETE operations
- **Rate Limiting:** MUST use middleware (e.g., AspNetCoreRateLimit) to prevent abuse

- **ContosoShop.Shared (class library, if used):** We have a small library for shared code, primarily to share model definitions between Client and Server. For example, the Order and OrderItem classes are defined in Shared, so both the server (when producing JSON) and the client (when decoding JSON) use the same definitions, reducing duplication and errors. We also share any validation or enums this way. (If this were a combined solution template, the Shared project is optional, but our solution does include it to illustrate good practice for code sharing).

### Project Structure:

- **Client/Pages** – Razor components for pages (e.g., Orders.razor, OrderDetails.razor, Inventory.razor, Support.razor, Login.razor).
- **Client/Services** – Service classes for API calls (e.g., OrderService that calls the API endpoints, encapsulating HttpClient use).
- **Client/Shared** – Shared UI components (e.g., a MainLayout, NavMenu, and maybe smaller components like an OrderCard).
- **Server/Controllers** – Web API controllers (e.g., OrdersController, possibly SupportController). Each controller corresponds to a set of endpoints for a resource. Our OrdersController handles /api/orders routes. We might not need a separate SupportController yet, but might use it i9n the future.
- **Server/Data** – EF Core DbContext and Configuration. Contains ContosoContext (our EF Core DbContext) and the entity classes (Order, OrderItem, etc., if not in Shared). Also likely contains code for seeding initial data into the SQLite DB on first run.
- **Server/Services** – Classes that encapsulate business logic, used by controllers. For example, an OrderService in the backend might contain methods like GetOrdersForUser(userId), ProcessReturn(orderId), etc. The controller can call these, which in turn call the DbContext and other services like EmailService. This layering isn't strictly needed in a small app but demonstrates how to separate concerns (especially if some logic is complex or reused).
- **Server/Utilities** – Utility classes (e.g., an EmailService interface and an implementation EmailServiceDev that logs emails). Also, configuration classes or helpers for mapping data.
- **Shared/Models** – Definitions for data models (Order, OrderItem, possibly an OrderStatus enum).

This structured approach makes it easier to maintain and test the app. For instance, one could unit-test OrderService methods independently of the controllers.

## 2. Frameworks and Libraries

- **.NET 8:** The entire solution is on .NET 8. Using .NET 8 ensures we have the latest C# features and performance improvements, and it aligns with the timelines of modern Azure services and the GitHub Copilot SDK (which expects a recent .NET runtime). .NET 8 is required to run this project, so ensure your environment is updated accordingly.

- **Blazor WebAssembly:** Our client is a Blazor WASM app. It runs the UI and client logic in the browser on WebAssembly, using Mono/WASM to execute C#. This means the user gets a rich interactive experience without constant page reloads. The Blazor app has been configured to call the backend for data. In Program.cs of the client, we register an HttpClient with the base address pointing to the server's URL so that HttpClient calls automatically target the correct domain (during dev, likely https://localhost:5001 for the API). We use dependency injection to provide services (like OrderService) to our components. The UI is built with Razor (which mixes HTML and C#). We've opted for a clean, Bootstrap-based styling (the default Blazor template's Bootstrap is used, giving us a responsive layout out of the box). No JavaScript frameworks are needed; however, we could interop with JS for things like copy-to-clipboard or other niceties if required.

- **ASP.NET Core Web API:** The server uses ASP.NET Core's minimal API/Controller approach. We created controllers with [ApiController] and routing attributes, returning strongly-typed models. For example, OrdersController.GetOrders() returns IEnumerable<Order> which ASP.NET Core automatically serializes to JSON. We rely on the default JSON (System.Text.Json) which is efficient and symmetric with Blazor's deserialization. CORS is configured to allow the Blazor client to call (when both run on same origin in dev, it's not an issue, but if separated, we allowed the client origin or used the fact it's hosted to avoid CORS issues).

- **Entity Framework Core (EF Core):** This is used for data access. The ContosoContext DbContext is configured with SQLite provider in development. We used code-first migrations to set up the database schema. The context has a DbSet<Order> and DbSet<OrderItem>, and possibly DbSet<User> if we had a user table (in our simplified case, user info might be minimal, but we can assume an in-memory user or a simple Users table with one entry). We run migrations on app startup (the app either ensures the SQLite DB exists or uses EnsureCreated in development). Entities have relationships (Order has a collection of OrderItems). EF Core tracking is used so when we update an Order's status and call SaveChanges(), it commits to the SQLite file.

  - **SQLite:** The connection string for SQLite is in appsettings.json (e.g., "ConnectionStrings": { "DefaultConnection": "Data Source=App_Data/ContosoShop.db" }). SQLite is chosen for local run because it requires no separate server installation and is lightweight. It is fully supported by EF Core. We included the .db file in the project so that it deploys if needed, and configured it to copy to output. In development mode, EF migrations are not automatically applied (we either ran them and checked in the DB, or we call context.Database.EnsureCreated() to auto-create tables for simplicity).

  - If we were to scale up or go to production, we would switch to Azure SQL. EF Core makes this easy: we'd change the UseSqlite to UseSqlServer with an Azure SQL connection string. Our code (repos, services) does not need to change. Migration to Azure SQL would involve deploying the migrations or generating a script—EF Core can handle differences in SQL dialects. Also, the app's repository pattern (if present) and service logic are database-agnostic beyond the configuration.

- **Logging and Configuration:** We use built-in .NET Logging (Microsoft.Extensions.Logging). In development, the default console logger is enabled so we see logs in the output. Configuration is done via the standard ASP.NET Core mechanism (appsettings files and environment variables). For example, the connection string and maybe a flag like "UseDevelopmentEmailService: true" are in appsettings.Development.json. In production (Azure), we'd likely override those with environment-specific values (Azure App Service application settings can directly override configuration keys). This means the app is prepared to accept config from Azure, such as a real SMTP server endpoint or an Azure Storage connection if we had one.

## 3. Backend: Key Components and Classes

**OrderController / OrderService:** This pair is responsible for all order-related endpoints with item-level return support. Key methods include:

- **GET /api/orders** – calls _orderService.GetOrdersForUser(userId) which returns a list from the DbContext (e.g., context.Orders.Include(o=>o.Items).ThenInclude(i=>i.Product).Where(o => o.UserId == userId)). The OrderService encapsulates business logic like filtering by user and sorting orders by date. This service is registered in DI with authentication-aware controllers.

- **GET /api/orders/{id}** – returns a single order with items and product details. Internally validates the order belongs to the authenticating user. Returns NotFound if not found, Forbidden if ownership check fails.

- **POST /api/orders/{id}/return** – DEPRECATED in favor of item-level returns

- **POST /api/orders/{orderId}/items/{itemId}/return** – the primary endpoint for item-level returns. Accepts ReturnItemRequest DTO with:
  - Quantity: number of units to return (validated against remaining returnable quantity)
  - Reason: justification for return (required, max 500 characters)
  
  The method flow:
  - Validates user owns the order (authorization check)
  - Validates order status is Delivered (business rule)
  - Validates requested quantity ≤ (original quantity - already returned quantity)
  - Calculates refund amount: original item price × return quantity
  - Creates OrderItemReturn record with quantity, reason, timestamp, and refund amount
  - Updates OrderItem.ReturnedQuantity += requested quantity
  - Calls _inventoryService.ReturnToInventoryAsync() to restore inventory (if ProductId exists)
  - Calls _emailService to send refund confirmation (PII-sanitized logging)
  - Returns success with refund details
  
- The OrderService properly handles multiple return transactions per item, tracking each return separately with its own justification and timestamp.

**InventoryController / InventoryService:** This pair manages the inventory tracking system:

- **InventoryService** provides three core operations:
  - **ReserveInventoryForOrderAsync(orderItems):** Automatically reserves inventory when orders are placed or reach Processing/Shipped/Delivered status. Uses FIFO logic (oldest items first by CreatedDate) to select available inventory, changes Status from "In Stock" to "Reserved", and updates LastStatusChange timestamp. Validates sufficient stock is available before reserving.
  
  - **ReturnToInventoryAsync(productId, quantity):** Called automatically during return processing. Finds Reserved inventory items for the product using FIFO logic (oldest reserved first by LastStatusChange), changes Status from "Reserved" back to "In Stock", sets HasReturnHistory = true, and updates LastStatusChange timestamp. Validates sufficient reserved inventory exists.
  
  - **GetAvailableStockAsync(productId):** Simple query returning count of inventory items with Status = "In Stock" for a given product.

- **InventoryController** exposes:
  - **GET /api/inventory** – Returns List<InventorySummary> for all products with calculated metrics:
    - TotalInventory: total count of InventoryItems for this product
    - AvailableStock: count where Status = "In Stock"
    - ReservedStock: count where Status = "Reserved"
    - ReturnedItems: count where HasReturnHistory = true
    - Product details: ItemNumber, Name, Price, Weight, Dimensions
  
  The endpoint uses LINQ projections for efficient database queries and requires authentication ([Authorize] attribute).

- **Integration with Orders:** The InventoryService is called by OrderService during return processing. When ProcessItemReturnAsync runs, it checks if the OrderItem has a ProductId, then calls _inventoryService.ReturnToInventoryAsync(productId, quantity) to automatically restore inventory. This ensures inventory counts stay synchronized with order returns.

- **Database Initialization:** DbInitializer.cs includes ReserveInventoryForOrdersAsync() method that runs during database seeding. It iterates through existing orders with Processing/Shipped/Delivered status and automatically reserves the appropriate inventory items, ensuring a consistent starting state.

**EmailService with Secure Logging:**

**SECURITY REQUIREMENT: Logs MUST NOT contain sensitive personal information (PII).**

We have an abstraction IEmailService with a method like SendEmail(to, subject, body). In Startup (or builder.Services setup), we register EmailServiceDev as the implementation for IEmailService.

**Development Email Service (EmailServiceDev):**
- In development, this service logs email operations to console for debugging
- **CRITICAL:** Logs MUST sanitize PII to comply with GDPR and data protection regulations
- **Secure Logging Implementation:**
  - Email addresses MUST be masked: `john.doe@example.com` → `j***@example.com` or use a hash
  - Order amounts MUST be logged generically: "Refund processed" instead of "Refunded $59.99"
  - Personal data (names, addresses) MUST NOT appear in logs
  - Use structured logging with sanitization: `_logger.LogInformation("Email sent: Type={EmailType}, Recipient={RecipientHash}", emailType, HashEmail(to))`
- Full email content MUST only be logged in secure audit tables (not console logs) if required for compliance

**Production Email Service:**
- In cloud deployment, use EmailServiceSendGrid or similar that uses SendGrid API
- Email sending MUST be performed over encrypted connections (TLS)
- API keys MUST be stored in Azure Key Vault, NOT in code or config files
- Failed email deliveries MUST be logged (without PII) and retried with exponential backoff
- Swap registration via config/environment detection - the DI pattern ensures no controller code changes

**Database Context and Entities:** We use EF Core code-first:

- **ContosoContext : DbContext** defines:
  - DbSet<Order> Orders
  - DbSet<OrderItem> OrderItems
  - DbSet<OrderItemReturn> OrderItemReturns
  - DbSet<Product> Products
  - DbSet<InventoryItem> InventoryItems
  - DbSet<User> Users (for authentication/authorization)
  
  The context includes comprehensive entity configuration with indexes, relationships, and constraints to ensure data integrity and performance.

- It's configured in Program.cs (Server) with something like:
  ```csharp
  builder.Services.AddDbContext<ContosoContext>(options =>
      options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
  ```

- In a cloud scenario, DefaultConnection can be changed to a SQL Server connection string and the code simply switched to UseSqlServer.

- We created a migration for the initial model (and any subsequent changes). In development, we ensure the SQLite DB has this schema. The solution includes the migration files under Server/Migrations/ for reference. For example, you might see migrations like InitialCreate, AddReturnedQuantityToOrderItem, AddOrderItemReturnTable, and AddInventorySystem which EF generated, showing the tables and schema evolution.

- **Order Entity:** fields like Id (int, primary key), UserId (could be int or GUID), OrderDate, Status (we use an OrderStatus enum in code but store as string or int in DB), TotalAmount (decimal). We might also have a DeliveryDate. The relationship: public List<OrderItem> Items to collect items. Using EF's [ForeignKey] or by naming convention, OrderItem has OrderId.

- **OrderItem Entity:** fields: Id (pk), OrderId (foreign key), ProductId (nullable int, foreign key to Product), ProductName (string), Quantity (int), Price (decimal), ReturnedQuantity (int, default 0). The ProductId is nullable for backward compatibility with existing orders, but new orders link to the Product catalog. The ReturnedQuantity tracks how many units of this item have been returned across all return transactions.

- **OrderItemReturn Entity:** fields: Id (pk), OrderItemId (foreign key), Quantity (int), Reason (string, max 500 chars, required), ReturnedDate (DateTime), RefundAmount (decimal). This table records individual return transactions, enabling item-level returns with justifications. Multiple return transactions can exist for the same OrderItem, allowing partial returns over time.

- **Product Entity:** fields: Id (pk), ItemNumber (unique string, max 50 chars), Name (string, max 200 chars), Price (decimal), Weight (decimal, in pounds), Dimensions (string, max 20 chars - Small/Medium/Large). This serves as the central product catalog. Products have navigation properties to OrderItems and InventoryItems. Item numbers follow format ITM-001 through ITM-025 in the seeded data.

- **InventoryItem Entity:** fields: Id (pk), ProductId (foreign key to Product), SerialNumber (unique string, max 50 chars), Status (string, max 20 chars - "In Stock"/"Reserved"/"Returned"), HasReturnHistory (bool, default false), CreatedDate (DateTime), LastStatusChange (DateTime nullable). Each inventory item represents a physical serialized unit. Serial numbers follow format ITM-XXX-YYYY where XXX is the item number and YYYY is the unit sequence (e.g., ITM-001-0042). The Status field tracks inventory state, and HasReturnHistory permanently flags items that have been returned at least once.

- **Entity Relationships:**
  - Order 1-to-many OrderItems
  - OrderItem many-to-1 Order
  - OrderItem many-to-1 Product (nullable for backward compatibility)
  - OrderItem 1-to-many OrderItemReturns
  - Product 1-to-many OrderItems
  - Product 1-to-many InventoryItems
  - InventoryItem many-to-1 Product

- The data seeder (in DbInitializer.cs) creates comprehensive test data including:
  - Two demo users: Mateo Gomez (mateo@contoso.com) and Megan Bowen (megan@contoso.com), both with password Password123!
  - 25 products in the catalog (ITM-001 through ITM-025) with realistic names, prices, weights, and dimensions
  - 2,500 inventory items (100 units per product) with unique serial numbers
  - ~10 orders per user (20 total) with various statuses (Processing, Shipped, Delivered, Returned)
  - Automatic inventory reservation for existing orders (83 items reserved from the initial seed data)
  - Order items linked to products via ProductId with catalog pricing
  - Order item return records for returned orders with justifications

- Because SQLite is file-based, when running the app, the DB file (ContosoShop.db) gets created in Server/App_Data/ or similar folder. This file persists data between runs unless deleted. The database initialization runs automatically on first startup, seeding all data including the complete inventory system.

**HTTP API Security, CORS, and Security Headers:**

**SECURITY REQUIREMENT: Comprehensive security middleware MUST be implemented.**

In Program.cs of Server, the following security measures MUST be configured:

**CORS Configuration:**
- CORS MUST be restrictively configured - DO NOT use `AllowAnyOrigin()`, `AllowAnyMethod()`, or `AllowAnyHeader()`
- MUST explicitly whitelist only required origins (e.g., `https://localhost:5002`, production domain)
- MUST explicitly whitelist only required HTTP methods (GET, POST only - not DELETE, PUT unless specifically needed)
- MUST explicitly whitelist only required headers (e.g., Content-Type, Authorization)
- Example secure CORS configuration:
  ```csharp
  policy.WithOrigins("https://localhost:5002", "https://yourdomain.com")
        .WithMethods("GET", "POST")
        .WithHeaders("Content-Type", "Authorization")
        .AllowCredentials();
  ```

**Security Headers Middleware:**
- MUST add security headers middleware to prevent common web vulnerabilities:
  - `X-Content-Type-Options: nosniff` (prevent MIME sniffing)
  - `X-Frame-Options: DENY` (prevent clickjacking)
  - `X-XSS-Protection: 1; mode=block` (XSS protection)
  - `Content-Security-Policy` (restrict resource loading)
  - `Strict-Transport-Security` (HSTS - already enabled via UseHsts())
- Can use NWebsec or custom middleware for header injection

**Authentication Middleware:**
- MUST call `app.UseAuthentication()` before `app.UseAuthorization()`
- MUST configure authentication scheme (JWT Bearer or Cookie)

**Rate Limiting Middleware:**
- MUST register and configure rate limiting services
- MUST apply rate limiting middleware in pipeline

**Database Security:**
- **SQLite Security:** If using SQLite in production, the database file MUST NOT be accessible via web (ensure App_Data is outside wwwroot)
- **Connection String Security:** Database credentials MUST be stored in Azure Key Vault (production) or User Secrets (development)
- **SQL Injection Prevention:** Continue using EF Core LINQ (no raw SQL) - already implemented correctly
- **Production Database:** For production deployments, MUST migrate to Azure SQL Database or SQL Server with:
  - Encrypted connections (SSL/TLS)
  - Firewall rules restricting access
  - Backup and disaster recovery configured
  - Transparent Data Encryption (TDE) enabled

## 4. Frontend: Key Components and Interaction with Backend

**State Management:** Blazor WASM allows us to use in-memory state for the current user's data. However, since our data is small, we fetch fresh data when needed rather than store it in a complex client-side state. For example:

- The Orders page, on initialization (OnInitializedAsync), calls OrderService.GetOrdersAsync() which GETs from the server and populates a local list orders. Blazor then renders the list. This retrieval happens each time the user navigates to Orders page (which ensures updated data if something changed). We could optimize by caching the result in a state container if needed, but not necessary for a few orders.

- The Order Details page likely receives an order ID via query parameter or route parameter (@page "/orders/{id:int}"). It then calls OrderService.GetOrderDetailsAsync(id) to retrieve the full order (or the Orders page might have passed the order in memory to avoid second call – but to keep it simple we do an API call here too, which would hit the DB again).

- After calling a return, we might refresh the data or at least update the bound objects to reflect new status. For instance, our OrderService.ReturnOrderAsync(id) calls the POST API. If it succeeds, we can either manually set the current order's status to Returned in the UI model (so the UI updates immediately) and perhaps even update the Orders list cached in memory (if we have it) so the list page is consistent. In our base flow, we simply navigate the user back to the orders list after a return and call the API again to load updated data – a simple and consistent approach.

**OrderService (Client side):** An Angular or React app would use a service or hook for API calls; similarly, in Blazor we created an OrderService class. This is registered via builder.Services.AddScoped<IOrderService, OrderService>() in Program.cs (Client). It wraps HttpClient calls:

- **GetOrdersAsync()** does `return await http.GetFromJsonAsync<List<Order>>("api/orders");`. Blazor's HttpClient is configured with base URI, so "api/orders" goes to the backend. The Shared models ensure the JSON maps to the Order class properly.

- **GetOrderDetailsAsync(id)** might call `http.GetFromJsonAsync<Order>($"api/orders/{id}")`.

- **ReturnOrderAsync(id)** would likely do `var response = await http.PostAsync($"api/orders/{id}/return", null); response.EnsureSuccessStatusCode();`. We didn't need to send a body, as the act of hitting the endpoint is enough. Alternatively, we could use PostAsJsonAsync if we needed to send data with the request (like a return reason). In base, not required.

- The service abstracts away those calls so our Razor components don't have to write boilerplate. In the component, we just do `await OrderService.ReturnOrderAsync(order.Id)` and handle exceptions if any.

**Razor Components:**

- **MainLayout.razor:** The main application layout component that provides:
  - **Title Bar:** Displays "ContosoShop Support Portal" on the far left (with left padding) and the authenticated user's name on the far right. Uses flexbox with `justify-content: space-between` to maintain proper spacing. The user name is displayed using `AuthenticationStateProvider` to fetch the current user's identity and only appears when the user is authenticated via `<AuthorizeView><Authorized>` wrapping. Font size is 1.5rem with font-weight 500 for readability.
  - **Responsive Design:** The title bar adjusts to screen sizes with proper alignment maintained across devices
  - **Navigation Integration:** Includes the NavMenu component in the sidebar

- **NavMenu.razor:** Navigation menu component with authentication-aware display:
  - **Top Section Menu Items (for authenticated users):**
    - Home (href="")
    - Orders (href="orders")
    - Contact Support (href="support")
    - Logout (button with HandleLogout handler)
  - **Bottom Section:**
    - View Inventory (href="inventory") - anchored to the very bottom of the navigation bar using `margin-top: auto` within a flexbox container. The nav uses `display: flex; height: 100%; justify-content: space-between;` to push this item to the bottom of the sidebar regardless of window height.
  - **Unauthenticated State:** Shows only the Login option when not authenticated
  - **Visual Structure:** Uses vertical flexbox layout with top menu items grouped in one container and View Inventory in a separate container at the bottom

- **Orders.razor:** Displays all orders for the authenticated user in a table format. Each order shows order number, date, total amount, and status with color-coded badges (Delivered = green, Shipped = blue, Processing = yellow, Returned = gray). Each order has a "View Details" link using `<NavLink href="@($"orders/{order.Id}")">`. The page includes authentication checks and redirects to login if not authenticated.

- **OrderDetails.razor:** Displays comprehensive details for a single order including:
  - Order metadata (number, date, status, delivery information)
  - Item-level listing with product names, quantities, original prices, and returned quantities
  - Per-item return buttons for delivered orders with remaining returnable quantity
  - Return form with quantity selector and reason text area (max 500 characters)
  - Return transaction history showing past returns with dates, quantities, reasons, and refund amounts
  - Refund confirmation message: "Your refund will be processed within 3 business days following receipt of the returned item(s)"
  - The component validates return requests client-side before submitting to API
  - Implements item-level return logic with OrderService.ProcessItemReturnAsync()

- **Inventory.razor:** New page for viewing inventory management data. Accessible via "View Inventory" navigation link (anchored at bottom of navigation menu). Displays:
  - **Summary Section:** Total products (25), total inventory items (2,500), available stock, reserved stock, and items with return history
  - **Detailed Table:** For each product shows:
    - Item Number (formatted as code)
    - Product Name, Price (currency formatted), Weight (lbs), Size badge
    - Total Inventory, Available (green badge), Reserved (yellow badge if > 0), Returned (blue badge if > 0)
    - Stock Status indicator: "In Stock" (green, >= 20 units), "Low Stock" (yellow, 1-19 units), "Out of Stock" (red, 0 units)
  - Data fetched from GET /api/inventory endpoint
  - Requires authentication ([Authorize] attribute)
  - Real-time counts reflect current order and return statuses

- **Support.razor:** Static customer support page with contact information. The base UI is ready to display responses from an AI agent to be enabled at a later date. We intentionally keep the design minimal here so it's easy to integrate dynamic behavior in the future.

**User Experience considerations:** We used Bootstrap for quick styling with custom flexbox layouts for precise positioning. The nav menu on the left (in NavMenu.razor) displays primary navigation links at the top (Home, Orders, Contact Support, Logout) with the "View Inventory" administrative link anchored to the bottom of the sidebar for easy access. The title bar shows the application name "ContosoShop Support Portal" on the left and the authenticated user's email address on the right, providing clear context about who is logged in. All navigation elements use authentication-aware `<AuthorizeView>` components to show appropriate options based on user state. The app is responsive (Bootstrap ensures the layout works on mobile; e.g., the nav collapses to a hamburger). This is not a major focus, but it means we are mobile-friendly out of the box.

## 5. Design for Local vs Cloud Environments

We've emphasized that the app is cloud-ready. Here are specific ways it's designed for easy migration:

**Separation of Concerns & Loose Coupling:** The clear split between front-end and back-end means we could scale them independently. In Azure, you could host the API on an App Service and the Blazor WASM on a CDN or Static Web App; they'd communicate over HTTPS. This separation follows the backend-for-frontend pattern and allows using Azure's best services for each (e.g., Azure CDN for static content, Azure App Service for the API logic). During local dev we combine them for convenience (the hosted Blazor model serving the static files), which is configurable via a flag. For instance, in Program.cs we might use app.UseBlazorFrameworkFiles() and app.MapFallbackToFile("index.html") on the server to serve the client app. This is active in dev; in a separate deployment, we could turn that off and deploy separately.

**Configuration and Secrets:** No secrets are needed for local run (we're not calling external APIs in base). But we have the infrastructure to introduce secrets via user secrets or environment variables if needed. For example, if using SendGrid, we'd store the API key in Azure's config and load it via Configuration["SendGridApiKey"]. The code might be ready to consume such config even if in base it's not set. In appsettings.json we keep sensitive things out (or in dev json only if not sensitive like a local filepath). This means pushing to Azure is just a matter of setting configurations appropriately.

**Database Migration Path:** Using SQLite in development is convenient, but for an Azure production, one would typically use Azure SQL. The EF Core migrations and model are fully compatible with SQL Server. The team could do one of:

- Use `dotnet ef database update` pointing to the Azure SQL connection to create schema.
- Or use EF Core's ability to generate a differential script and run that on Azure SQL.
- Also consider using Azure DevOps or GitHub Actions pipeline to apply migrations during deployment (ensuring zero downtime strategies, etc.). The code doesn't need changes - it's devops process.
- The codebase includes some conditional logic if needed (like maybe a compiler directive or config flag to choose UseSqlServer vs UseSqlite). More simply, we might rely on the connection string format to determine provider; but in practice, we can let the lab environment always use SQLite. Documenting the path: "Switching to Azure SQL involves adding the Microsoft.EntityFrameworkCore.SqlServer NuGet package and changing one line in Program.cs (UseSqlServer). Then update the DefaultConnection string to the Azure SQL connection string in production settings. That's it." This highlights ease of migration.

**Scalability and Performance:** For a local lab, performance is a non-issue. But the use of EF Core (with appropriate indexing if needed) and streaming of data in Web API (we're returning all orders at once, which is fine for small numbers; pagination could be added for very large histories), and the efficient static content loading from Blazor's published output all mean the app can handle typical load. On Azure, enabling response compression, and using Azure Front Door or CDN for static files could vastly improve global performance. None of these require code changes, just configuration and Azure toggles. For instance, ASP.NET Core by default has gzip compression (if enabled in config) for API responses; we can ensure it's on in production.

**Azure Integration Points:** We considered possible Azure services:

- **Azure App Service:** Ideal for hosting the ASP.NET Core API (and even the Blazor client). We ensure the app writes logs to console (which App Service can capture) and doesn't write to disk (except the SQLite DB which is in the content folder; on App Service that's fine but in production we'd use Azure SQL to avoid file write).

- **Azure Static Web Apps:** If splitting client, this could host the Blazor WASM and provide an auto CI/CD from a GitHub repo. Meanwhile, an Azure Function or App Service could host the API. We'd then configure CORS accordingly. The code would not change except possibly the base addresses.

- **Azure Monitor/Application Insights:** We can add Application Insights SDK to monitor server performance and track requests, which is straightforward with one line addition in Program (builder.Services.AddApplicationInsightsTelemetry()). We have not included it in base (to avoid extra setup for lab) but it's an easy add that doesn't alter our logic.

In summary, the tech stack is contemporary and robust: C# full-stack (Blazor + ASP.NET Core) with EF Core for ORM, targeting .NET 8 or later for best performance and features. All these choices align directly with Microsoft's cloud offerings, making the journey from a local SQLite/VS Code experience to an Azure-deployed, scalable solution very smooth. We've enforced clean separation and used interfaces/DI for things like email and data access to ensure that improving or changing implementations (like switching to Azure services) is just a matter of configuration or adding new classes, not rewriting core features.
