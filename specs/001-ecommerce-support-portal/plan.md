# Implementation Plan: ContosoShop E-commerce Support Portal (Security Enhanced)

**Branch**: `001-ecommerce-support-portal` | **Date**: 2026-02-06 (Updated) | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-ecommerce-support-portal/spec.md` (v2.0 - Security Enhanced with Inventory Management)

**Note**: This plan has been updated to address comprehensive security requirements from Constitution v2.0.0 and stakeholder documents, plus item-level returns and inventory management features.

## Summary

Build a secure e-commerce customer support portal that enables authenticated users to view their orders, check status, and initiate item-level returns through a self-service interface. The application implements comprehensive security controls including authentication, authorization, CSRF protection, rate limiting, input validation, secure logging (PII sanitization), security headers, and restrictive CORS configuration. The system includes a complete inventory management system with 2,500 serialized items across 25 products, automatic inventory reservation for orders, and inventory restoration for returns. The system uses Blazor WebAssembly for the client, ASP.NET Core Web API for the backend, and Entity Framework Core with SQLite (development) / Azure SQL (production). Security is enforced at every layer per Constitution v2.0.0 Security-First Design principle.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**Primary Dependencies**: 
- ASP.NET Core 8 Web API (backend REST API)
- Blazor WebAssembly (frontend SPA)
- Entity Framework Core 8 (ORM with SQLite/Azure SQL)
- ASP.NET Core Identity (authentication & user management)
- AspNetCoreRateLimit (rate limiting middleware)
- NWebsec or custom middleware (security headers)

**Storage**: SQLite (development), Azure SQL Database (production) - Entity Framework Core abstracts provider  
**Testing**: xUnit for unit tests, integration tests for API contracts, Playwright/bUnit for UI tests  
**Target Platform**: Cross-platform (.NET 8) - Windows, Linux, macOS development; Azure App Service deployment  
**Project Type**: Web application (Blazor WASM + ASP.NET Core API hosted model)  
**Performance Goals**: 
- API response time <500ms (p95) under normal load
- Authentication <2 seconds
- Order list load <2 seconds  
- Order details load <1 second
- Return operation <30 seconds end-to-end

**Constraints**: 
- HTTPS mandatory (HTTP redirects to HTTPS)
- Session timeout 30 minutes
- Rate limits enforced (auth: 5 failures/15min, orders: 60 req/min, details: 120 req/min, returns: 10 req/hour)
- PII MUST NOT appear in logs
- All secrets externalized (User Secrets dev, Azure Key Vault production)
- EF Core LINQ only (no raw SQL)

**Scale/Scope**: 
- MVP: Single tenant demo (seeded users for testing)
- Expected volume: 100s of users, 1000s of orders, 2500 inventory items
- Production-ready architecture supports horizontal scaling on Azure
- Database: 6 core entities (User, Order, OrderItem, OrderItemReturn, Product, InventoryItem)
- API endpoints: ~15 endpoints (auth, orders, item-level returns, inventory, support)
- UI pages: 7 pages (login, orders list, order details with item-level returns, inventory management, support, home)
- Inventory: 25 products, 2,500 serialized items with automatic reservation/restoration

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Constitution Version**: v2.0.0 (Security Enhanced)

### Principle I: Security-First Design ✅ PASS

- ✅ **Authentication**: ASP.NET Core Identity implementation planned with password hashing (bcrypt/PBKDF2)
- ✅ **Authorization**: `[Authorize]` attributes on all protected endpoints, ownership validation (`order.UserId == currentUserId`)
- ✅ **CSRF Protection**: Manual anti-forgery validation via `IAntiforgery.ValidateRequestAsync()` on API endpoints for all POST/PUT/DELETE
- ✅ **Rate Limiting**: AspNetCoreRateLimit middleware configured with spec-defined limits
- ✅ **Input Validation**: Model validation attributes + server-side enforcement, EF Core LINQ only
- ✅ **Secure Logging**: PII sanitization implemented (email masking, no financial amounts in logs)
- ✅ **Security Headers**: X-Frame-Options, CSP, HSTS, X-Content-Type-Options, X-XSS-Protection
- ✅ **Transport Security**: HTTPS enforced, HTTP redirects, HSTS enabled
- ✅ **Secrets Management**: User Secrets (dev), Azure Key Vault (prod), no secrets in source control

**Justification**: All mandatory security controls from Constitution v2.0.0 are included in the implementation plan.

### Principle II: Testable Architecture ✅ PASS

- ✅ **Dependency Injection**: All services registered in DI container (IOrderService, IInventoryService, IEmailService, etc.)
- ✅ **Interfaces**: IOrderService, IInventoryService, IEmailService abstractions enable mocking
- ✅ **Separation of Concerns**: Controllers → Services → DbContext layering
- ✅ **Single Responsibility**: OrderService handles order/return logic, InventoryService handles inventory operations, EmailService handles notifications
- ✅ **Mock Implementations**: EmailServiceDev for development/testing
- ✅ **Integration Tests**: API contract tests for authentication, authorization, CSRF, rate limiting, inventory coordination

**Justification**: Architecture follows DI patterns enabling comprehensive test coverage including inventory management integration.

### Principle III: Code Quality Standards ✅ PASS

- ✅ **.NET Conventions**: Following C# naming conventions and coding standards
- ✅ **XML Documentation**: All public APIs documented per requirements
- ✅ **Code Reviews**: Pull request workflow enforced
- ✅ **Static Analysis**: Compiler warnings treated as errors
- ✅ **Consistent Formatting**: EditorConfig configured
- ✅ **No TODOs**: All decisions documented in spec/plan, no placeholder comments

**Justification**: Standard .NET quality practices enforced throughout.

### Principle IV: Cloud-Ready Design ✅ PASS

- ✅ **Configuration Externalized**: appsettings.json with environment overrides
- ✅ **Secrets Externalized**: User Secrets (dev), Azure Key Vault with Managed Identity (prod)
- ✅ **Database Abstraction**: EF Core enables SQLite → Azure SQL migration
- ✅ **Service Abstraction**: IEmailService enables console → SendGrid swap
- ✅ **CORS Security**: Explicit whitelist (no wildcards), specific methods/headers only
- ✅ **Database Security**: TDE enabled, firewall rules, private endpoints planned for production
- ✅ **Static Resources**: Blazor WASM deployable to CDN/Azure Static Web Apps
- ✅ **Logging**: Console/structured output compatible with Azure Monitor
- ✅ **Network Security**: VNet integration, private endpoints supported

**Justification**: Architecture supports seamless Azure deployment with enterprise security.

### Principle V: API-Driven Development ✅ PASS

- ✅ **REST Endpoints**: Well-defined API contracts with clear HTTP semantics
- ✅ **Consistent Status Codes**: 200, 400, 401, 403, 404, 429, 500 per spec
- ✅ **Shared Models**: ContosoShop.Shared project for DTOs
- ✅ **Security Enforcement**: All endpoints use `[Authorize]` and validate ownership
- ✅ **API Documentation**: OpenAPI/Swagger configured
- ✅ **Error Responses**: RFC 7807 problem details format
- ✅ **Contract Tests**: Integration tests verify API contracts and security

**Justification**: API-first design with mandatory security enforcement on all endpoints.

**GATE RESULT**: ✅ **PASS** - All constitutional principles satisfied. Proceed to Phase 0 research.

## Project Structure

### Documentation (this feature)

```text
specs/001-ecommerce-support-portal/
├── plan.md              # This file (implementation plan with security architecture)
├── spec.md              # Feature specification v2.0 (security enhanced)
├── research.md          # Phase 0 output (technology decisions)
├── data-model.md        # Phase 1 output (entity definitions)
├── quickstart.md        # Phase 1 output (getting started guide)
├── contracts/           # Phase 1 output (OpenAPI specifications)
│   ├── README.md
│   └── openapi.yaml
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoShop.Server/         # ASP.NET Core Web API (backend)
├── Controllers/
│   ├── OrdersController.cs
│   └── InventoryController.cs
├── Services/
│   ├── IOrderService.cs
│   ├── OrderService.cs
│   ├── IInventoryService.cs
│   ├── InventoryService.cs
│   ├── IEmailService.cs
│   ├── EmailServiceDev.cs
│   └── PiiSanitizingLogger.cs
├── Data/
│   ├── ContosoContext.cs
│   └── DbInitializer.cs
├── Middleware/
│   └── SecurityHeadersMiddleware.cs (if using custom)
├── Program.cs              # DI configuration, middleware pipeline
├── appsettings.json
├── appsettings.Development.json
└── ContosoShop.Server.csproj

ContosoShop.Client/         # Blazor WebAssembly (frontend)
├── Pages/
│   ├── Index.razor
│   ├── Login.razor
│   ├── Orders.razor
│   ├── OrderDetails.razor
│   ├── Inventory.razor
│   └── Support.razor
├── Shared/
│   ├── MainLayout.razor
│   ├── NavMenu.razor
│   └── OrderStatusBadge.razor
├── Services/
│   ├── IOrderService.cs
│   └── OrderService.cs
├── Layout/
│   ├── MainLayout.razor    # Main layout with title bar (app name left, user name right) and sidebar
│   └── NavMenu.razor       # Navigation menu with View Inventory anchored at bottom
├── wwwroot/
│   ├── css/
│   └── index.html
├── Program.cs              # DI configuration
└── ContosoShop.Client.csproj

ContosoShop.Shared/         # Shared models and contracts
├── Models/
│   ├── User.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── OrderItemReturn.cs
│   ├── Product.cs
│   ├── InventoryItem.cs
│   ├── OrderStatus.cs
│   └── LoginModels.cs
├── DTOs/
│   ├── InventorySummary.cs
│   └── ReturnItemRequest.cs
└── ContosoShop.Shared.csproj

Tests/                      # Test projects
├── ContosoShop.Server.Tests/
│   ├── Controllers/
│   │   ├── OrdersControllerTests.cs
│   │   └── InventoryControllerTests.cs
│   ├── Services/
│   │   ├── OrderServiceTests.cs
│   │   └── InventoryServiceTests.cs
│   └── Integration/
│       ├── AuthenticationTests.cs
│       ├── AuthorizationTests.cs
│       ├── CsrfProtectionTests.cs
│       ├── RateLimitingTests.cs
│       └── InventoryCoordinationTests.cs
└── ContosoShop.Client.Tests/
    └── (bUnit component tests if needed)

.specify/                   # Specification infrastructure
├── memory/
│   └── constitution.md
├── scripts/
└── templates/

StakeholderDocuments/       # Business requirements
├── ProjectGoals.md
├── AppFeatures.md
└── TechStack.md
```

**Structure Decision**: Using Blazor WebAssembly Hosted architecture with three projects:
1. **ContosoShop.Server**: ASP.NET Core Web API hosting the backend and serving the Blazor client
2. **ContosoShop.Client**: Blazor WASM SPA for the user interface
3. **ContosoShop.Shared**: Shared DTOs, models, and contracts

This structure supports the security requirements by:
- Clear separation of concerns (presentation, business logic, data access)
- Enables independent testing of authentication, authorization, and business logic
- Supports deployment flexibility (combined or separate deployments)
- Facilitates security layer enforcement at API boundary

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Status**: No violations - All constitutional principles satisfied.

The implementation plan fully aligns with Constitution v2.0.0 requirements. All security controls, testing practices, code quality standards, cloud-ready patterns, and API-driven development principles are incorporated into the design.

---

## Phase 0: Research & Technology Decisions

**Status**: ✅ COMPLETE

All technology decisions have been made based on the specification requirements and Constitution v2.0.0:

### Authentication Technology Decision

**Decision**: ASP.NET Core Identity  
**Rationale**: 
- Built-in support for password hashing with industry-standard algorithms
- Native integration with EF Core for user storage
- Provides cookie-based authentication with HttpOnly, SameSite attributes, and SecurePolicy=SameAsRequest (secure in production HTTPS, works in development HTTP)
- Supports role-based authorization (future extensibility)
- Well-documented and battle-tested in production environments
- Reduces custom security code (less risk of security bugs)

**Alternatives Considered**:
- JWT tokens: More complex for web apps with server-side rendering needs; better for mobile/SPA-only scenarios
- Custom authentication: High security risk, not recommended per security best practices

**Client-Side Authentication State Management**:
- Uses `CookieAuthenticationStateProvider` to check server-side authentication status
- Calls `GET /api/auth/user` endpoint to retrieve current user authentication state and basic info
- Home page and protected pages check authentication in `OnInitializedAsync()` and redirect to `/login` if not authenticated
- Uses `forceLoad: true` on navigation to ensure complete page reload and proper cookie validation
- Navigation menu uses `<AuthorizeView>` component to display different options for authenticated vs. unauthenticated users
- **Title Bar Layout:** MainLayout.razor displays "ContosoShop Support Portal" on the far left with the authenticated user's email on the far right using flexbox (`justify-content: space-between`). User name fetched from `AuthenticationStateProvider` and displayed only when authenticated.
- **Navigation Menu Structure:** NavMenu.razor uses vertical flexbox with `height: 100%` to enable bottom anchoring:
  - **Top section:** Primary navigation items (Home, Orders, Contact Support, Logout) grouped together
  - **Bottom section:** View Inventory link anchored at the very bottom using `margin-top: auto`, visually separating administrative features from primary navigation
  - Menu items automatically adjust based on authentication state (Login only when unauthenticated, full menu when authenticated)
- Requires `Microsoft.AspNetCore.Components.Authorization` package (v8.0.2) in client project
- Pattern ensures authentication is enforced client-side in addition to server-side API protection

### Rate Limiting Technology Decision

**Decision**: AspNetCoreRateLimit  
**Rationale**:
- De facto standard for .NET rate limiting
- Supports IP-based and client ID-based limiting
- Configurable per-endpoint limits
- In-memory and distributed cache support (Redis for production scaling)
- Active maintenance and community support

**Alternatives Considered**:
- Custom middleware: Reinventing the wheel, higher development cost
- Built-in .NET 7+ rate limiting: Too new, less flexible than AspNetCoreRateLimit

### Security Headers Technology Decision

**Decision**: NWebsec middleware  
**Rationale**:
- Comprehensive security header support
- Easy configuration via fluent API or middleware
- Supports Content-Security-Policy, X-Frame-Options, HSTS, etc.
- Active maintenance
- Used by many production .NET applications

**Alternatives Considered**:
- Custom middleware: Feasible but more code to maintain
- Manual headers in Program.cs: Works but less maintainable

### Database Technology Decision

**Decision**: SQLite (development), Azure SQL (production)  
**Rationale**:
- SQLite: Zero configuration, file-based, perfect for local development
- Azure SQL: Enterprise-grade security (TDE, firewall rules, private endpoints)
- EF Core abstracts provider difference (same LINQ queries work for both)
- Migration path is configuration change only

**Alternatives Considered**:
- PostgreSQL: Excellent choice but Azure SQL has better Azure ecosystem integration
- SQL Server LocalDB: More heavyweight than SQLite for development

### Logging Strategy Decision

**Decision**: Microsoft.Extensions.Logging with custom PII sanitization  
**Rationale**:
- Built into .NET, no additional dependencies
- Structured logging support (JSON output for Azure Monitor)
- Easy to implement PII sanitization via custom log filters/formatters
- Consistent across application

**Implementation**: Create custom ILogger wrapper that sanitizes email addresses (hash or mask) and removes financial amounts before logging.

---

## Phase 1: Design & Contracts

### Data Model

**See**: [data-model.md](data-model.md) (generated separately)

**Summary**: Three core entities with security-focused design:
- **User**: Id, Name, Email (unique), PasswordHash, Orders (navigation), CreatedAt
- **Order**: Id, UserId (FK for authorization), OrderDate, Status, TotalAmount, ShipDate, DeliveryDate, Items (navigation), User (navigation)
- **OrderItem**: Id, OrderId (FK), ProductName, Quantity, Price, Order (navigation)

All entities include proper foreign keys for referential integrity and authorization checks.

### API Contracts

**See**: [contracts/openapi.yaml](contracts/openapi.yaml) (generated separately)

**Summary of Endpoints**:

**Authentication**:
- `POST /api/auth/login` - Authenticate user (rate limited: 5 failures/15min)
- `POST /api/auth/logout` - Terminate session

**Orders** (all require `[Authorize]`):
- `GET /api/orders` - List authenticated user's orders (rate limit: 60/min)
- `GET /api/orders/{id}` - Get order details with ownership validation (rate limit: 120/min)
- `POST /api/orders/{id}/return` - Initiate return with CSRF validation (rate limit: 10/hour)

**Security Headers**: All responses include X-Frame-Options, CSP, HSTS, X-Content-Type-Options, X-XSS-Protection

**CORS**: Whitelist `https://localhost:5002` (dev), production domain (prod) - GET, POST methods only - Content-Type, Authorization headers only

### Architecture Decisions

**Authentication Flow**:
1. User submits credentials to POST /api/auth/login
2. Server validates against User table (PasswordHash comparison)
3. On success: Issue authentication cookie (HttpOnly, Secure, SameSite)
4. On failure: Log attempt, increment rate limit counter, return 401
5. All subsequent requests include cookie, validated by `[Authorize]` attribute

**Authorization Pattern**:
```csharp
// In OrdersController.GetOrder(int id)
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
var order = await _context.Orders
    .Include(o => o.Items)
    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
    
if (order == null)
    return NotFound(); // Or 403 if order exists but wrong user
```

**CSRF Protection Pattern** (Web API Manual Validation):
```csharp
[HttpPost("{id}/return")]
public async Task<IActionResult> ReturnOrder(int id)
{
    // Validate CSRF token manually for API endpoint
    // Note: [ValidateAntiForgeryToken] attribute is MVC-specific and doesn't work for Web API controllers
    try
    {
        await _antiforgery.ValidateRequestAsync(HttpContext);
    }
    catch (AntiforgeryValidationException)
    {
        return BadRequest(new { detail = "Invalid or missing anti-forgery token." });
    }
    
    // Then perform authorization check
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    // ...
}
```

**Rate Limiting Configuration**:
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "POST:/api/auth/login",
        "Period": "15m",
        "Limit": 5
      },
      {
        "Endpoint": "GET:/api/orders",
        "Period": "1m",
        "Limit": 60
      }
    ]
  }
}
```

**PII Sanitization Pattern**:
```csharp
public class PiiSanitizingLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, 
        Exception exception, Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);
        // Regex to mask email: j***@example.com
        message = Regex.Replace(message, @"(\w)\w+@", "$1***@");
        // Remove dollar amounts: replace "$XX.XX" with "[AMOUNT]"
        message = Regex.Replace(message, @"\$\d+\.\d{2}", "[AMOUNT]");
        _innerLogger.Log(logLevel, eventId, message, exception, (s, e) => s);
    }
}
```

### Agent Context Update

Running agent context update script to add new security technologies...

---

## Phase 2: Task Planning

**Status**: ⏳ PENDING - Run `/speckit.tasks` to generate detailed task breakdown

The tasks will be organized by user story:
- User Story 1: Authentication implementation (login, logout, session management, rate limiting)
- User Story 2: Order listing with authorization
- User Story 3: Order details with authorization
- User Story 4: Returns with CSRF protection
- User Story 5: Support page (static content)
- Cross-cutting: Security headers, CORS, PII sanitization, testing

---

## Post-Design Constitution Re-Check

*Re-evaluating constitutional compliance after design phase*

### Principle I: Security-First Design ✅ PASS (RE-CONFIRMED)

All security controls have concrete implementation plans:
- ✅ Authentication: ASP.NET Core Identity with password hashing
- ✅ Authorization: Ownership validation in all endpoints
- ✅ CSRF: Manual IAntiforgery validation on state-changing API operations (client fetches token, includes in X-CSRF-TOKEN header)
- ✅ Rate Limiting: AspNetCoreRateLimit with per-endpoint configuration
- ✅ Secure Logging: Custom PII sanitization logger wrapper
- ✅ Security Headers: NWebsec middleware configured
- ✅ CORS: Explicit whitelist configuration documented

### Principle II: Testable Architecture ✅ PASS (RE-CONFIRMED)

- ✅ DI: All services use interfaces (IOrderService, IEmailService)
- ✅ Layer Separation: Controllers → Services → DbContext
- ✅ Mock Support: EmailServiceDev for testing
- ✅ Integration Tests: Planned for auth, authz, CSRF, rate limiting

### Principle III: Code Quality Standards ✅ PASS (RE-CONFIRMED)

- ✅ XML documentation required for all public APIs
- ✅ .NET conventions enforced
- ✅ Code review process via PR workflow

### Principle IV: Cloud-Ready Design ✅ PASS (RE-CONFIRMED)

- ✅ Configuration externalized (appsettings.json, User Secrets, Key Vault)
- ✅ Database provider abstraction (SQLite ↔ Azure SQL)
- ✅ Service abstraction (EmailServiceDev ↔ SendGrid)
- ✅ Azure deployment ready (App Service, Static Web Apps, Azure SQL)

### Principle V: API-Driven Development ✅ PASS (RE-CONFIRMED)

- ✅ REST contracts defined in OpenAPI spec
- ✅ Security enforcement on all endpoints
- ✅ Shared models in ContosoShop.Shared project
- ✅ Integration tests for API contracts

**FINAL GATE RESULT**: ✅ **PASS** - Design fully complies with Constitution v2.0.0. Ready for task breakdown (`/speckit.tasks`).

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
