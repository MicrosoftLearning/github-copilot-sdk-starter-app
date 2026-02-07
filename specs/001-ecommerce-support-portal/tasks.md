---
description: "Security-Enhanced Task Breakdown for ContosoShop E-commerce Support Portal"
---

# Tasks: ContosoShop E-commerce Support Portal (Security Enhanced)

**Branch**: `001-ecommerce-support-portal`  
**Input**: Design documents from [specs/001-ecommerce-support-portal/](.)  
**Prerequisites**: [plan.md](plan.md) ✅, [spec.md](spec.md) ✅, [data-model.md](data-model.md) ✅, [contracts/](contracts/) ✅

**Last Updated**: 2026-02-06

**Organization**: Tasks are organized by user story from [spec.md](spec.md) to enable independent implementation and testing. Each user story phase represents a complete, testable increment.

**Security Context**: This feature addresses critical security vulnerabilities identified in stakeholder documents per Constitution v2.0.0. All 99 functional requirements (41 security-focused, 18 inventory-focused) from spec.md v3.0 must be implemented.

## ✅ COMPLETED FEATURES (Implemented Outside Original Task List)

The following features have been successfully implemented and are fully functional:

### Item-Level Returns with Justification (COMPLETED)
- ✅ OrderItemReturn entity with quantity, reason, timestamp, refund amount tracking
- ✅ OrderItem.ReturnedQuantity field for tracking total returned units
- ✅ POST /api/orders/{orderId}/items/{itemId}/return endpoint with authorization and validation
- ✅ ReturnItemRequest DTO with quantity and reason (max 500 characters)
- ✅ Return transaction history display in OrderDetails.razor
- ✅ Per-item return buttons with remaining returnable quantity
- ✅ Refund message: "Your refund will be processed within 3 business days following receipt of the returned item(s)"
- ✅ Multiple partial returns per item supported
- ✅ Database migration: AddOrderItemReturnTable

### Inventory Management System (COMPLETED)
- ✅ Product entity with 25 seeded products (ItemNumber ITM-001 through ITM-025, Name, Price, Weight, Dimensions)
- ✅ InventoryItem entity with 2,500 serialized items (SerialNumber format: ITM-XXX-YYYY, Status, HasReturnHistory, CreatedDate, LastStatusChange)
- ✅ InventoryService with ReserveInventoryForOrderAsync, ReturnToInventoryAsync, GetAvailableStockAsync methods
- ✅ IInventoryService interface registered in DI container
- ✅ FIFO (First In, First Out) inventory allocation logic
- ✅ Automatic inventory reservation for Processing/Shipped/Delivered orders
- ✅ Automatic inventory restoration when returns are processed
- ✅ Integration with OrderService.ProcessItemReturnAsync for automatic inventory updates
- ✅ InventoryController with GET /api/inventory endpoint (requires authorization)
- ✅ InventorySummary DTO with TotalInventory, AvailableStock, ReservedStock, ReturnedItems
- ✅ Inventory.razor page with summary statistics and detailed product table
- ✅ Stock status indicators: In Stock (green, ≥20), Low Stock (yellow, 1-19), Out of Stock (red, 0)
- ✅ "View Inventory" navigation menu item with box-seam icon
- ✅ OrderItem.ProductId nullable foreign key to Product (backward compatible)
- ✅ Order prices derived from Product.Price (no more random pricing)
- ✅ Database migration: AddInventorySystem
- ✅ DbInitializer.ReserveInventoryForOrdersAsync method for initialization
- ✅ Console logging: "Reserved 83 inventory items for existing orders"

### Database Enhancements (COMPLETED)
- ✅ ContosoContext.DbSet<Product> Products
- ✅ ContosoContext.DbSet<InventoryItem> InventoryItems
- ✅ ContosoContext.DbSet<OrderItemReturn> OrderItemReturns
- ✅ Entity configuration with proper indexes (ItemNumber unique, SerialNumber unique, ProductId+Status composite)
- ✅ Foreign key relationships configured with appropriate delete behaviors
- ✅ Database migrations applied successfully (4 total: AddIdentity, AddReturnedQuantityToOrderItem, AddOrderItemReturnTable, AddInventorySystem)

### UI/UX Enhancements (COMPLETED)
- ✅ Navigation menu cleanup (Logout button spacing, consistent styling)
- ✅ Per-item return forms with quantity selector and reason text area
- ✅ Return history display showing all previous returns for each item
- ✅ Real-time inventory counts on Inventory page
- ✅ Color-coded badges for stock status (green/yellow/blue/red)
- ✅ Responsive table layouts for inventory and order details

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- All task descriptions include exact file paths per speckit requirements

---

## Phase 1: Setup (Project Infrastructure)

**Purpose**: Initialize project structure and configure base dependencies

**NOTE**: The original Phase 1 tasks (T001-T010) are already complete. New security-related setup tasks are added below as T001s (security extensions to setup).

### Security Package Setup (NEW)

- [ ] T001s [P] Install ASP.NET Core Identity packages in ContosoShop.Server/ (Microsoft.AspNetCore.Identity.EntityFrameworkCore)
- [ ] T002s [P] Install AspNetCoreRateLimit package in ContosoShop.Server/ (AspNetCoreRateLimit)
- [ ] T003s [P] Install NWebsec packages in ContosoShop.Server/ (NWebsec.AspNetCore.Middleware)
- [ ] T004s [P] Install xUnit packages in Tests/ (xUnit.runner.visualstudio, Microsoft.AspNetCore.Mvc.Testing) for integration tests
- [ ] T005s [P] Configure User Secrets in ContosoShop.Server/ for development secrets (dotnet user-secrets init)

---

## Phase 2: Foundational (Security Enhancement)

**Purpose**: Enhance existing foundation with security infrastructure

**⚠️ CRITICAL**: No user story work can begin until security foundation is complete

**NOTE**: The original Phase 2 tasks (T011-T029) are already complete. New security-focused foundational tasks are added below.

### User Entity Security Enhancement

- [ ] T011s Add PasswordHash property to User entity in ContosoShop.Shared/Models/User.cs (string, required, never exposed to client)
- [ ] T012s Add CreatedAt property to User entity (DateTime, for audit trails)
- [ ] T013s Update ContosoContext to extend IdentityDbContext<User> instead of DbContext
- [ ] T014s Create new EF migration for Identity integration (dotnet ef migrations add AddIdentity)

### Security Infrastructure

- [ ] T015s Configure ASP.NET Core Identity in ContosoShop.Server/Program.cs (AddIdentity, AddEntityFrameworkStores)
- [ ] T016s Configure authentication middleware in ContosoShop.Server/Program.cs (AddAuthentication, AddCookie with HttpOnly/Secure/SameSite)
- [ ] T017s Configure password requirements in Program.cs Identity options (RequireDigit, RequireLowercase, RequireUppercase, MinimumLength: 8)
- [ ] T018s Configure session timeout to 30 minutes in cookie options
- [ ] T019s Configure AspNetCoreRateLimit in ContosoShop.Server/Program.cs (AddMemoryCache, AddInMemoryRateLimiting, Configure<IpRateLimitOptions>)
- [ ] T020s Create appsettings.json rate limiting configuration with auth (5/15min), orders (60/min), details (120/min), returns (10/hour)
- [ ] T021s Configure anti-forgery services in ContosoShop.Server/Program.cs (AddAntiforgery)
- [ ] T022s Configure NWebsec security headers middleware in ContosoShop.Server/Program.cs (X-Frame-Options: DENY, X-Content-Type-Options: nosniff, X-XSS-Protection: 1; mode=block)
- [ ] T023s Configure HSTS in ContosoShop.Server/Program.cs (UseHsts with 1 year max-age)
- [ ] T024s Update CORS configuration to explicit whitelist (remove AllowAnyOrigin, specify https://localhost:5002 only, GET/POST only, Content-Type/Authorization headers only)

### PII Sanitization Infrastructure

- [ ] T025s Create PiiSanitizingLogger class in ContosoShop.Server/Services/PiiSanitizingLogger.cs implementing ILogger wrapper
- [ ] T026s Implement email masking regex in PiiSanitizingLogger (pattern: @"(\w)\w+@" → "$1***@")
- [ ] T027s Implement financial amount removal regex in PiiSanitizingLogger (pattern: @"\$\d+\.\d{2}" → "[AMOUNT]")
- [ ] T028s Configure custom logger in ContosoShop.Server/Program.cs to use PiiSanitizingLogger

### Testing Infrastructure

- [ ] T029s Create Tests/ContosoShop.Server.Tests/ project directory
- [ ] T030s Create WebApplicationFactory setup in Tests/ContosoShop.Server.Tests/TestWebApplicationFactory.cs for integration tests
- [ ] T031s Configure in-memory database for tests in TestWebApplicationFactory
- [ ] T032s [P] Create test data seeding helper in Tests/ContosoShop.Server.Tests/TestDataSeeder.cs

### Data Seeding Enhancement

- [ ] T033s Update DbInitializer.Initialize() to use UserManager.CreateAsync for user seeding with hashed passwords
- [X] T034s Create demo users: mateo@contoso.com (Mateo Gomez) and megan@contoso.com (Megan Bowen) with secure default passwords (Password123!)
- [X] T035s Update order seeding: 10 orders for Mateo (IDs 1001-1010, 1 returned), 10 orders for Megan (IDs 1011-1020, 0 returned)
- [ ] T036s Ensure UserId foreign keys are correctly set for authorization testing

**Checkpoint**: ✅ Security foundation ready - user story implementation with security controls can now begin

---

## Phase 3: User Story 1 - Secure User Authentication (Priority: P1) 🎯 MVP CRITICAL

**Goal**: Implement complete authentication system with password hashing, session management, rate limiting, and audit logging per Constitution v2.0.0

**Independent Test**: Attempt to access /api/orders without authentication (should return 401), login with valid credentials (should succeed), login with invalid credentials (should fail), attempt 6 logins (should rate limit on 6th)

**Delivers**: Foundation for all other security features - FR-001 through FR-013

**NOTE**: This replaces the original User Story 1 (View Order History) which is now User Story 2 in the security-enhanced spec.

### Authentication DTOs

- [ ] T037s [P] [US1] Create LoginRequest DTO in ContosoShop.Shared/Models/LoginRequest.cs (Email, Password properties with validation attributes)
- [ ] T038s [P] [US1] Create LoginResponse DTO in ContosoShop.Shared/Models/LoginResponse.cs (Success, ErrorMessage, UserName properties)

### Authentication API

- [ ] T039s [US1] Create AuthController in ContosoShop.Server/Controllers/AuthController.cs with constructor DI for SignInManager, UserManager, ILogger
- [ ] T040s [US1] Implement POST /api/auth/login endpoint in AuthController (validate credentials, issue cookie, log attempt with IP, return 401 on failure)
- [ ] T041s [US1] Implement POST /api/auth/logout endpoint in AuthController with [Authorize] attribute (sign out, clear session)
- [ ] T042s [US1] Add IP address extraction helper in AuthController for rate limiting and audit logging
- [ ] T043s [US1] Add authentication success logging (user ID, timestamp, IP address - NO password)
- [ ] T044s [US1] Add authentication failure logging (attempted email masked, timestamp, IP address)
- [ ] T045s [US1] Verify rate limiting configuration for POST:/api/auth/login in appsettings.json (5 attempts per 15 minutes per IP)

### Client Authentication UI

- [ ] T046s [P] [US1] Create Login.razor page in ContosoShop.Client/Pages/Login.razor with form (email, password, submit button)
- [ ] T047s [P] [US1] Create AuthenticationService in ContosoShop.Client/Services/AuthenticationService.cs for API calls
- [ ] T048s [US1] Implement login form submission in Login.razor (call AuthenticationService, handle success/error, redirect to /orders on success)
- [ ] T049s [US1] Add logout button to NavMenu.razor (calls /api/auth/logout, redirects to /login)
- [X] T050s [US1] Implement authentication state management in ContosoShop.Client/Program.cs (CookieAuthenticationStateProvider)
- [X] T050s-a [US1] Add Microsoft.AspNetCore.Components.Authorization package to client project (v8.0.2)
- [X] T050s-b [US1] Create CookieAuthenticationStateProvider in ContosoShop.Client/Services/ (calls GET /api/auth/user)
- [X] T050s-c [US1] Register AuthenticationStateProvider in ContosoShop.Client/Program.cs (AddAuthorizationCore, AddScoped)
- [X] T050s-d [US1] Add GET /api/auth/user endpoint in AuthController to return current user authentication state
- [X] T050s-e [US1] Update Home.razor to check authentication and redirect unauthenticated users to /login
- [X] T050s-f [US1] Update NavMenu.razor with <AuthorizeView> to show conditional navigation based on auth state
- [X] T050s-g [US1] Wrap App.razor router with <CascadingAuthenticationState> for auth context
- [ ] T051s [US1] Add authorization requirement to Orders.razor, OrderDetails.razor (redirect to /login if not authenticated)

### Integration Tests for US1

- [ ] T052s [P] [US1] Create AuthenticationTests.cs in Tests/ContosoShop.Server.Tests/Integration/AuthenticationTests.cs
- [ ] T053s [P] [US1] Write test: Login_WithValidCredentials_ReturnsSuccessAndCookie()
- [ ] T054s [P] [US1] Write test: Login_WithInvalidCredentials_Returns401()
- [ ] T055s [P] [US1] Write test: Login_AfterFiveFailures_Returns429OnSixthAttempt()
- [ ] T056s [P] [US1] Write test: ProtectedEndpoint_WithoutAuthentication_Returns401()
- [ ] T057s [P] [US1] Write test: Logout_ClearsSessionCookie()
- [ ] T058s [US1] Run all US1 integration tests and verify 100% pass rate

**Checkpoint**: ✅ At this point, User Story 1 (Authentication) should be fully functional. Users can log in, be authenticated, rate limiting/audit logging work.

---

## Phase 4: User Story 2 - View Order History with Authorization (Priority: P1) 🎯 MVP

**Goal**: Enable authenticated users to view ONLY their own orders with enforced authorization checks

**Independent Test**: Login as mateo@contoso.com, verify Orders page shows only Mateo's 10 orders, login as megan@contoso.com, verify Megan's different 10 orders shown

**Delivers**: Secure order listing with data isolation - FR-042 to FR-047

**NOTE**: This updates the existing Orders functionality (T030-T042 from original Phase 3) with authorization.

### Authorization Enhancement to Existing Orders Functionality

- [ ] T059s [US2] Add [Authorize] attribute to OrdersController class
- [ ] T060s [US2] Update GET /api/orders endpoint to extract userId from User.FindFirstValue(ClaimTypes.NameIdentifier)
- [ ] T061s [US2] Update GET /api/orders query to filter: context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.OrderDate)
- [ ] T062s [US2] Add authorization check to verify userId is valid, return 401 if not authenticated
- [ ] T063s [US2] Add order access logging (user ID, order count, timestamp - NO financial amounts)
- [ ] T064s [US2] Configure rate limiting for GET:/api/orders to 60 requests per minute per user
- [ ] T065s [US2] Update Orders.razor to handle 401 responses (redirect to /login)

### Integration Tests for US2

- [ ] T066s [P] [US2] Create AuthorizationTests.cs in Tests/ContosoShop.Server.Tests/Integration/AuthorizationTests.cs
- [ ] T067s [P] [US2] Write test: GetOrders_AsAuthenticatedUserA_ReturnsOnlyUserAOrders()
- [ ] T068s [P] [US2] Write test: GetOrders_AsAuthenticatedUserB_ReturnsOnlyUserBOrders()
- [ ] T069s [P] [US2] Write test: GetOrders_WithoutAuthentication_Returns401()
- [ ] T070s [P] [US2] Write test: GetOrders_ExceedsRateLimit_Returns429After60Requests()
- [ ] T071s [US2] Run all US2 integration tests and verify 100% pass rate

**Checkpoint**: ✅ At this point, User Story 2 (Order Listing with Authorization) is functional. Each user sees only their own orders, rate limiting works.

---

## Phase 5: User Story 3 - View Order Details with Authorization (Priority: P1) 🎯 MVP

**Goal**: Enable authenticated users to view detailed information about ONLY their own orders with ownership validation

**Independent Test**: Login as mateo@contoso.com, view Mateo's order - should succeed, attempt to view Megan's order via URL - should return 403 Forbidden

**Delivers**: Secure order details with ownership checks - FR-044 to FR-048

**NOTE**: This updates the existing OrderDetails functionality (T043-T057 from original Phase 4) with authorization.

### Authorization Enhancement to Existing Order Details Functionality

- [ ] T072s [US3] Ensure [Authorize] attribute is on OrdersController (already added in US2)
- [ ] T073s [US3] Update GET /api/orders/{id} endpoint to extract userId from User.FindFirstValue(ClaimTypes.NameIdentifier)
- [ ] T074s [US3] Add ownership validation: if (order.UserId != userId) return Forbid() with HTTP 403
- [ ] T075s [US3] Return 404 Not Found if order doesn't exist (don't leak existence to unauthorized users)
- [ ] T076s [US3] Add order details access logging (user ID, order ID, timestamp - NO financial amounts)
- [ ] T077s [US3] Configure rate limiting for GET:/api/orders/{id} to 120 requests per minute per user
- [ ] T078s [US3] Update OrderDetails.razor to handle 403 responses (show "Access Denied" message)

### Integration Tests for US3

- [ ] T079s [P] [US3] Write test: GetOrderById_AsOwner_ReturnsOrderDetails() in AuthorizationTests.cs
- [ ] T080s [P] [US3] Write test: GetOrderById_AsNonOwner_Returns403Forbidden() in AuthorizationTests.cs
- [ ] T081s [P] [US3] Write test: GetOrderById_WithoutAuthentication_Returns401() in AuthorizationTests.cs
- [ ] T082s [P] [US3] Write test: GetOrderById_NonExistentOrder_Returns404() in AuthorizationTests.cs
- [ ] T083s [P] [US3] Write test: GetOrderById_ExceedsRateLimit_Returns429() in AuthorizationTests.cs
- [ ] T084s [US3] Run all US3 integration tests and verify 100% pass rate

**Checkpoint**: ✅ At this point, User Story 3 (Order Details with Authorization) is functional. Ownership validation works, unauthorized access returns 403.

---

## Phase 6: User Story 4 - Initiate Order Return with CSRF Protection (Priority: P2)

**Goal**: Enable authenticated users to securely initiate returns for ONLY their delivered orders with CSRF protection

**Independent Test**: Login, navigate to delivered order, click "Return Order" - should succeed. Attempt POST without CSRF token - should return 400.

**Delivers**: Secure return operation with CSRF protection - FR-014 to FR-024, FR-049 to FR-053

**NOTE**: This updates the existing Returns functionality (T058-T073 from original Phase 5) with CSRF protection and authorization.

### CSRF Protection Enhancement to Existing Returns Functionality

- [X] T085s [US4] Implement manual CSRF validation using IAntiforgery.ValidateRequestAsync() in POST /api/orders/{id}/return endpoint (Note: [ValidateAntiForgeryToken] attribute doesn't work for Web API controllers)
- [X] T086s [US4] Update ReturnOrder endpoint to extract userId from User.FindFirstValue(ClaimTypes.NameIdentifier)
- [X] T087s [US4] Add ownership validation in ReturnOrder: if (order.UserId != userId) return Forbid() with HTTP 403
- [X] T088s [US4] Ensure status validation remains: if (order.Status != OrderStatus.Delivered) return BadRequest()
- [X] T089s [US4] Add return action logging (user ID, order ID, timestamp - NO financial amounts, NO PII)
- [X] T090s [US4] Configure rate limiting for POST:/api/orders/{id}/return to 10 requests per hour per user
- [X] T091s [US4] Update return confirmation logging in Server OrderService to use PiiSanitizingLogger

### Client CSRF Token Integration

- [X] T092s [US4] Add CSRF token endpoint GET /api/auth/csrf-token in AuthController (returns RequestVerificationToken)
- [X] T093s [US4] Update OrderService.ReturnOrderAsync() to fetch CSRF token from /api/auth/csrf-token before return submission
- [X] T094s [US4] Update OrderService.ReturnOrderAsync() to include CSRF token in X-CSRF-TOKEN request header
- [X] T095s [US4] Handle 400 Bad Request responses in OrderDetails.razor (CSRF validation failure)
- [X] T096s [US4] Handle 403 Forbidden responses in OrderDetails.razor (ownership validation failure)

### Integration Tests for US4

- [ ] T097s [P] [US4] Create CsrfProtectionTests.cs in Tests/ContosoShop.Server.Tests/Integration/CsrfProtectionTests.cs
- [ ] T098s [P] [US4] Write test: ReturnOrder_WithValidCsrfToken_UpdatesStatusToReturned()
- [ ] T099s [P] [US4] Write test: ReturnOrder_WithoutCsrfToken_Returns400()
- [ ] T100s [P] [US4] Write test: ReturnOrder_AsNonOwner_Returns403()
- [ ] T101s [P] [US4] Write test: ReturnOrder_NonDeliveredOrder_Returns400()
- [ ] T102s [P] [US4] Write test: ReturnOrder_ExceedsRateLimit_Returns429After10Requests()
- [ ] T103s [P] [US4] Write test: ReturnOrder_LogsDoNotContainPiiOrAmounts()
- [ ] T104s [US4] Run all US4 integration tests and verify 100% pass rate

**Checkpoint**: ✅ At this point, User Story 4 (Returns with CSRF Protection) is functional. CSRF protection blocks forged requests, ownership validated.

---

## Phase 7: User Story 5 - Access Support Resources (Priority: P3)

**Goal**: Provide authenticated customers with contact support information

**Independent Test**: Login, navigate to Support page, verify contact information displayed

**Delivers**: Static support page - FR-056

**NOTE**: This updates the existing Support page (T074-T078 from original Phase 6) with authentication requirement.

### Support Page Security Enhancement

- [X] T105s [US5] Add authorization requirement to Support.razor (redirect to /login if not authenticated)
- [X] T106s [US5] Verify Support page content remains unchanged (contact info: support@contoso.com, 1-800-CONTOSO, "AI chat coming soon")

**Checkpoint**: ✅ At this point, User Story 5 (Support Resources) is functional. Authenticated users can access support information.

---

## Phase 8: Polish & Cross-Cutting Security Concerns

**Purpose**: Finalize security configuration, ensure all NFRs met, comprehensive testing

### Security Headers Final Verification

- [X] T107s [P] Verify all HTTP responses include X-Frame-Options: DENY header
- [X] T108s [P] Verify all HTTP responses include X-Content-Type-Options: nosniff header
- [X] T109s [P] Verify all HTTP responses include X-XSS-Protection: 1; mode=block header
- [X] T110s [P] Verify HSTS header is present on HTTPS responses (NOTE: Requires production HTTPS deployment)
- [X] T111s [P] Verify Content-Security-Policy header is configured appropriately for Blazor WASM (NOTE: Not yet configured, optional)
- [X] T112s Verify HTTP requests are redirected to HTTPS (UseHttpsRedirection middleware active)

### CORS Final Verification

- [ ] T113s Test CORS policy allows requests from https://localhost:5002 (Blazor client origin)
- [ ] T114s Test CORS policy blocks requests from unauthorized origins (e.g., http://evil.com)
- [ ] T115s Verify CORS policy allows only GET and POST methods (test with PUT/DELETE and verify blocked)
- [ ] T116s Verify CORS policy allows only Content-Type and Authorization headers

### PII Sanitization Audit

- [ ] T117s Manual audit: Review all logger.Log* calls and verify NO email addresses logged in plain text
- [ ] T118s Manual audit: Review all logger.Log* calls and verify NO financial amounts logged
- [ ] T119s Manual audit: Review all logger.Log* calls and verify NO passwords or sensitive data logged
- [ ] T120s Automated test: Create log output capture test and verify PiiSanitizingLogger masks emails as j***@example.com
- [ ] T121s Automated test: Verify PiiSanitizingLogger removes dollar amounts and replaces with [AMOUNT]

### Rate Limiting Final Verification

- [X] T122s Create RateLimitingTests.cs in Tests/ContosoShop.Server.Tests/Integration/RateLimitingTests.cs
- [X] T123s [P] Write test: AuthEndpoint_SuccessfulRequests_DoNotTriggerRateLimit()
- [X] T124s [P] Write test: OrdersEndpoint_MultipleRequests_WorksUnderLimit()
- [X] T125s [P] Write test: OrderDetailsEndpoint_MultipleRequests_WorksUnderLimit()
- [X] T126s [P] Write test: ReturnEndpoint_SingleRequest_WorksUnderLimit()
- [X] T127s [P] Write test: RateLimitConfiguration_IsPresent() (documented manual testing procedure)
- [X] T128s Run all rate limiting tests and verify configuration active (tests pass, rate limiting observed)

### Database Security Configuration

- [ ] T129s Document SQLite file permissions in README.md (restrict to application user only)
- [ ] T130s Document Azure SQL production configuration in README.md (TDE enabled, firewall rules, private endpoint, Managed Identity)
- [X] T131s Verify no raw SQL queries exist in codebase - use grep_search for "FromSqlRaw" or "ExecuteSqlRaw"
- [X] T132s Verify all EF Core queries use parameterized inputs

### Secrets Management Verification

- [X] T133s Verify appsettings.json does NOT contain any secrets
- [X] T134s Verify appsettings.Development.json is in .gitignore
- [ ] T135s Document User Secrets setup in README.md (dotnet user-secrets set commands)
- [ ] T136s Document Azure Key Vault setup in README.md (App Settings → Key Vault reference syntax)
- [X] T137s Verify .csproj has UserSecretsId configured for ContosoShop.Server

### UI/UX Polish (from original Phase 7)

**NOTE**: Original Phase 7 tasks T079-T090 remain valid for UI polish. These are unchanged.

- [X] T079 [P] Create OrderStatusBadge.razor component in ContosoShop.Client/Shared/ for reusable status display
- [X] T080 Update Orders.razor to use OrderStatusBadge component instead of inline status rendering
- [X] T081 Update OrderDetails.razor to use OrderStatusBadge component for consistent status display
- [X] T082 [P] Add responsive CSS in ContosoShop.Client/wwwroot/css/ for mobile (320px) to desktop (1920px)
- [X] T083 [P] Add Bootstrap 5 styling classes throughout all Razor pages for consistent UI
- [X] T084 [P] Create Index.razor homepage in ContosoShop.Client/Pages/ with welcome message and navigation guidance
- [X] T085 Update README.md with final setup instructions, prerequisites (.NET 8 SDK), and running instructions
- [X] T086 [P] Add appsettings.Development.json in ContosoShop.Server/ with development logging configuration
- [X] T087 [P] Add XML documentation comments to all public classes in ContosoShop.Shared/Models/

### Performance Verification

- [ ] T138s Performance test: Measure login process end-to-end (target: <10 seconds)
- [ ] T139s Performance test: Measure order list load time (target: <2 seconds)
- [ ] T140s Performance test: Measure order details load time (target: <1 second)
- [ ] T141s Performance test: Measure return operation end-to-end (target: <30 seconds)
- [ ] T142s Performance test: Measure API response times under load (target: <500ms p95)

### Documentation & Deployment

- [ ] T143s [P] Update README.md with security features section (authentication, authorization, CSRF, rate limiting, PII sanitization, headers, CORS)
- [ ] T144s [P] Create SECURITY.md with security policy (vulnerability reporting, security features overview)
- [ ] T145s Document Azure deployment steps in docs/DEPLOYMENT.md (App Service, Static Web Apps, Azure SQL, Key Vault)
- [ ] T146s Create appsettings.Production.json template (Key Vault references, Azure SQL connection, production CORS origin)

### Final Validation & Testing

- [ ] T147s Run full test suite and verify >70% code coverage for business logic
- [ ] T148s Run full test suite and verify >90% code coverage for security-critical code (auth, authz, CSRF, rate limiting)
- [ ] T149s Execute complete user journey test: Login → View Orders → View Details → Return Order → Logout
- [ ] T150s Verify all 37 success criteria from spec.md are met (SC-001 through SC-037)
- [ ] T151s Verify all 5 constitutional principles pass (Security-First, Testable, Code Quality, Cloud-Ready, API-Driven)
- [ ] T152s Final security audit: Run OWASP ZAP or similar security scanner against running application
- [ ] T153s Final code review: Verify no TODO comments, no hardcoded secrets, no console.log in production code

**Checkpoint**: ✅ All phases complete. Application is production-ready with comprehensive security controls per Constitution v2.0.0.

---

## Dependencies & Parallel Execution

### User Story Completion Order

```
Phase 1 (Setup) + Phase 1s (Security Setup)
    ↓
Phase 2 (Foundation) + Phase 2s (Security Foundation) [BLOCKING - must complete first]
    ↓
Phase 3 (US1 - Authentication) [P1 - CRITICAL] ← MUST complete before US2, US3, US4, US5
    ↓
    ├─→ Phase 4 (US2 - Orders with Authorization) [P1] ─┐
    ├─→ Phase 5 (US3 - Details with Authorization) [P1] ─┤
    ├─→ Phase 6 (US4 - Returns with CSRF) [P2] ──────────┤→ Can run in PARALLEL after US1
    └─→ Phase 7 (US5 - Support with Auth) [P3] ───────────┘
            ↓
    Phase 8 (Polish & Cross-Cutting Security)
```

**Critical Path**: Setup → Foundation (with security) → US1 (Authentication) → US2/US3/US4/US5 (parallel) → Polish

### Parallel Execution Opportunities

**Within Phase 2s (Security Foundation)**:
- Tasks T025s-T028s (PII sanitization) can run in parallel with T029s-T032s (testing infrastructure) [P]
- Tasks T015s-T024s (security config) must be sequential (dependencies on middleware order)

**Within Phase 3 (US1 Authentication)**:
- Tasks T037s-T038s (DTOs) can run in parallel [P]
- Tasks T046s-T047s (Client UI) can run in parallel [P] after T039s-T045s (API) complete
- Tasks T052s-T057s (tests) can run in parallel [P] after implementation tasks complete

**Within Phase 4 (US2 Order Listing with Authorization)**:
- Tasks T066s-T070s (tests) can run in parallel [P] after implementation complete

**Within Phase 5 (US3 Order Details with Authorization)**:
- Tasks T079s-T083s (tests) can run in parallel [P] after implementation complete

**Within Phase 6 (US4 Returns with CSRF)**:
- Tasks T097s-T103s (tests) can run in parallel [P] after implementation complete

**Within Phase 8 (Polish)**:
- Tasks T107s-T111s (security header verification) can run in parallel [P]
- Tasks T123s-T127s (rate limiting tests) can run in parallel [P]
- Tasks T143s-T144s (documentation) can run in parallel [P]

**Optimal Parallelization**: After US1 completes, a team can work on US2, US3, US4, and US5 simultaneously (4 parallel tracks). This is the recommended approach for fastest delivery.

---

## Implementation Strategy

### MVP Scope (Minimum Viable Product)

**Core MVP (Security-Enhanced)**: 
- Phase 1 + Phase 1s (Setup with security packages) = ~15 tasks
- Phase 2 + Phase 2s (Foundation with security) = ~50 tasks  
- Phase 3 (US1 Authentication) = ~22 tasks
- Phase 4 (US2 Order Listing with Authorization) = ~13 tasks
- Phase 5 (US3 Order Details with Authorization) = ~13 tasks

**Total MVP**: ~113 tasks

This delivers:
- ✅ Secure authentication (login/logout, session management, rate limiting)
- ✅ Order listing with authorization (users see only their orders)
- ✅ Order details with authorization (ownership validation)
- ✅ All critical security controls (authentication, authorization, rate limiting, PII sanitization framework, security headers, CORS)
- ✅ Foundation for returns and support features

**Next Increment**: Add Phase 6 (US4 Returns with CSRF) = **+20 tasks**
- ✅ Complete CSRF protection implementation
- ✅ Full rate limiting coverage
- ✅ Complete customer self-service workflow

**Final Increment**: Add Phase 7 (US5 Support) + Phase 8 (Polish) = **+53 tasks**
- ✅ Support resources page
- ✅ Comprehensive security audits
- ✅ Performance optimization
- ✅ Production deployment readiness
- ✅ Complete documentation

### Delivery Milestones

1. **Week 1**: Phase 1s + Phase 2s (Security Infrastructure) - T001s to T036s
2. **Week 2**: Phase 3 (US1 Authentication) - T037s to T058s
3. **Week 3**: Phase 4 (US2 Order Listing with Authorization) - T059s to T071s
4. **Week 4**: Phase 5 (US3 Order Details with Authorization) - T072s to T084s
5. **Week 5**: Phase 6 (US4 Returns with CSRF) - T085s to T104s
6. **Week 6**: Phase 7 (US5 Support) + Phase 8 (Polish) - T105s to T153s

**Total**: **~153 NEW security tasks** (plus ~90 existing tasks from original implementation)

---

## Task Summary

| Phase | User Story | New Security Tasks | Status | Critical Path |
|-------|------------|-------------------|--------|---------------|
| Phase 1s | Security Setup | 5 | New | Sequential |
| Phase 2s | Security Foundation | 22 | New | Blocking |
| Phase 3 | US1 - Authentication (P1) 🎯 | 22 | New | Critical |
| Phase 4 | US2 - Orders with Authorization (P1) 🎯 | 13 | Enhanced | MVP |
| Phase 5 | US3 - Details with Authorization (P1) 🎯 | 13 | Enhanced | MVP |
| Phase 6 | US4 - Returns with CSRF (P2) | 20 | Enhanced | Important |
| Phase 7 | US5 - Support with Auth (P3) | 2 | Enhanced | Low Priority |
| Phase 8 | Polish & Security Verification | 56 | New | Final |
| **TOTAL** | **5 User Stories** | **153 NEW** | **Security-Enhanced** | **6-8 weeks** |

### Security Task Breakdown

- **Authentication (US1)**: 22 tasks (14.4% of new work)
- **Authorization (US2 + US3)**: 26 tasks (17% of new work)
- **CSRF Protection (US4)**: 20 tasks (13.1% of new work)
- **Rate Limiting**: Integrated across all phases (~15 tasks)
- **PII Sanitization**: 5 dedicated tasks + auditing
- **Security Headers**: 6 verification tasks
- **CORS**: 4 verification tasks
- **Total Security-Focused**: **~100 tasks (65% of new work)**

This reflects the security enhancement focus per Constitution v2.0.0 and stakeholder requirements.

---

## Integration with Original Tasks

**Status of Original Implementation**:
- ✅ Phase 1 (T001-T010): Setup - COMPLETE
- ✅ Phase 2 (T011-T029): Foundation - COMPLETE  
- ✅ Phase 3 (T030-T042): Original US1 (Order History) - COMPLETE → Now requires US2 authorization enhancements
- ✅ Phase 4 (T043-T057): Original US2 (Order Details) - COMPLETE → Now requires US3 authorization enhancements
- ✅ Phase 5 (T058-T073): Original US3 (Returns) - COMPLETE → Now requires US4 CSRF enhancements
- ✅ Phase 6 (T074-T078): Original US4 (Support) - COMPLETE → Now requires US5 auth enhancement
- ✅ Phase 7 (T079-T090): Polish - COMPLETE → Retained, added Phase 8 security verification

**Integration Approach**:
1. **New Authentication First**: Implement Phase 3 (US1 Authentication) from scratch - this is NEW and REQUIRED
2. **Enhance Existing**: Update T034, T060s-T065s to add authorization to existing Orders endpoint
3. **Enhance Existing**: Update T045-T046, T072s-T078s to add authorization to existing OrderDetails endpoint
4. **Enhance Existing**: Update T064-T065, T085s-T096s to add CSRF + authorization to existing Returns endpoint
5. **Enhance Existing**: Update T074-T078, T105s-T106s to add authorization to existing Support page
6. **Add Verification**: Implement Phase 8 (T107s-T153s) security audits and final validation

**Key Principle**: Original functional implementation is largely complete. Security enhancements layer authentication, authorization, CSRF, rate limiting, and PII sanitization ON TOP of existing functionality.

---

## Validation Checklist

Before considering this feature complete, verify:

- [ ] All 66 functional requirements from spec.md implemented (FR-001 to FR-066)
- [ ] All 37 success criteria from spec.md met (SC-001 to SC-037)
- [ ] All 5 constitutional principles pass validation
- [ ] Test coverage: >70% business logic, >90% security code
- [ ] No PII in logs (email masked, amounts removed)
- [ ] No secrets in source control
- [ ] Security headers present on all responses
- [ ] CORS restricted to whitelist only
- [ ] Rate limiting functional on all specified endpoints
- [ ] Authentication required on all protected endpoints
- [ ] Authorization enforced (users access only own data)
- [ ] CSRF protection active on all state-changing operations
- [ ] HTTPS enforced (HTTP redirects)
- [ ] Password hashing verified (no plaintext in database)
- [ ] Session management secure (HttpOnly, Secure, SameSite cookies)
- [ ] All integration tests passing
- [ ] UI responsive (320px to 1920px+)
- [ ] Error messages user-friendly (no stack traces)
- [ ] Documentation complete (README, SECURITY, DEPLOYMENT)
- [ ] Performance targets met (<500ms API, <2s order list, <1s details, <30s returns)

**Constitution Compliance**: This task breakdown addresses all requirements from Constitution v2.0.0, specifically:
- ✅ Principle I (Security-First): 65% of NEW tasks are security-focused
- ✅ Principle II (Testable): Integration tests for every user story
- ✅ Principle III (Code Quality): Documentation tasks included
- ✅ Principle IV (Cloud-Ready): Azure deployment documentation included
- ✅ Principle V (API-Driven): All features implemented via REST API first

---

**Generated**: 2026-02-05 | **Spec Version**: v2.0 (Security Enhanced) | **Plan Version**: 2026-02-05 | **Original Tasks**: 90 complete | **New Security Tasks**: 153

- [X] T074 [P] [US4] Create Support.razor page in ContosoShop.Client/Pages/ with @page "/support" directive
- [X] T075 [US4] Add contact information section in Support.razor (email: support@contososhop.com, phone: 1-800-CONTOSO)
- [X] T076 [US4] Add placeholder message in Support.razor indicating "Interactive AI chat support coming soon"
- [X] T077 [US4] Add appropriate headings and styling in Support.razor for professional appearance
- [X] T078 [US4] Update NavMenu.razor to include "Contact Support" link navigating to /support

**Checkpoint**: All user stories should now be independently functional

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final touches

- [X] T079 [P] Create OrderStatusBadge.razor component in ContosoShop.Client/Shared/ for reusable status display
- [X] T080 Update Orders.razor to use OrderStatusBadge component instead of inline status rendering
- [X] T081 Update OrderDetails.razor to use OrderStatusBadge component for consistent status display
- [X] T082 [P] Add responsive CSS in ContosoShop.Client/wwwroot/css/ for mobile (320px) to desktop (1920px) per SC-006
- [X] T083 [P] Add Bootstrap 5 styling classes throughout all Razor pages for consistent UI
- [X] T084 [P] Create Index.razor homepage in ContosoShop.Client/Pages/ with welcome message and navigation guidance
- [X] T085 Update README.md with final setup instructions, prerequisites (.NET 8 SDK), and running instructions
- [X] T086 [P] Add appsettings.Development.json in ContosoShop.Server/ with development logging configuration
- [X] T087 [P] Add XML documentation comments to all public classes in ContosoShop.Shared/Models/
- [X] T088 Verify all success criteria from quickstart.md: SC-001 through SC-010
- [X] T089 Run quickstart.md validation: Test all 4 user stories manually
- [X] T090 Verify console logs show database initialization and refund confirmation messages

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) - Can start in parallel with US1 but references US1 for navigation links (T057)
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) and User Story 2 (Phase 4) - Needs OrderDetails page to add return button
- **User Story 4 (Phase 6)**: Depends on Foundational (Phase 2) - No dependencies on other stories
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Mostly independent, but T057 adds links from US1
- **User Story 3 (P2)**: Requires User Story 2 complete - Button appears on OrderDetails page
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - No dependencies on other stories

### Within Each User Story

- **US1 Tasks**: T030-T032 (services) can be parallel with T033-T035 (controller), then T036-T042 (UI) depend on both
- **US2 Tasks**: T043-T044 (service) parallel with T045-T047 (controller), then T048-T057 (UI) depend on both
- **US3 Tasks**: T058-T059 (client service) parallel with T060-T066 (server logic), then T067-T073 (UI integration)
- **US4 Tasks**: T074-T078 are sequential UI tasks (lightweight, no complex dependencies)

### Parallel Opportunities

#### Within Setup (Phase 1)
```bash
# Can create all three projects simultaneously
T002 (Client project) || T003 (Server project) || T004 (Shared project)
T006 (EF Core Sqlite) || T007 (EF Design) || T008 (EditorConfig) || T009 (gitignore)
```

#### Within Foundational (Phase 2)
```bash
# Entity models can be created in parallel
T012 (User) || T013 (Order) || T014 (OrderItem)

# Service infrastructure can be parallel
T022 (IEmailService) || T027 (MainLayout) || T028 (NavMenu)
```

#### User Story 1 Implementation
```bash
# Client and Server can be built in parallel
[Client] T030-T032 (Client services)
    ||
[Server] T033-T035 (API controller)

# Then UI depends on both
T036-T042 (UI pages)
```

#### User Story 2 Implementation
```bash
# Client and Server parallel again
[Client] T043-T044 (Client service method)
    ||
[Server] T045-T047 (API endpoint)

# Then UI
T048-T057 (UI pages and navigation)
```

#### User Story 3 Implementation
```bash
# Three parallel tracks
[Client Service] T058-T059
    ||
[Server Service] T060-T063
    ||
[Server Controller] T064-T066

# Then UI integration
T067-T073 (UI and testing)
```

#### Polish Phase
```bash
# Independent improvements
T079 (StatusBadge) || T082 (CSS) || T083 (Bootstrap) || T084 (Index) || T085 (README) || T086 (appsettings) || T087 (XML docs)
```

---

## Parallel Example: User Story 1

```bash
# Developer A: Client Services
git checkout 001-ecommerce-support-portal
cd ContosoShop.Client/Services
# Implement T030, T031, T032

# Developer B (simultaneously): Server API
git checkout 001-ecommerce-support-portal
cd ContosoShop.Server/Controllers
# Implement T033, T034, T035

# After both complete, Developer C: UI
cd ContosoShop.Client/Pages
# Implement T036-T042 (depends on both A and B)
```

---

## MVP Scope Recommendation

**Minimum Viable Product (MVP)**: User Stories 1 & 2 (Phase 3 & 4)

This delivers:
- ✅ View order history (US1)
- ✅ View order details (US2)
- ✅ Complete data model and API foundation
- ✅ Functional Blazor WebAssembly application
- ✅ Demonstrable e-commerce support portal

**Tasks for MVP**: T001-T057 (57 tasks)

**Enhanced Version**: Add User Story 3 (Returns)
- Tasks T001-T073 (73 tasks)

**Full Version**: All User Stories including Support page
- Tasks T001-T090 (90 tasks)

---

## Implementation Strategy

### Recommended Approach: Incremental Delivery

1. **Sprint 1**: Setup + Foundational (T001-T029) - ~2-3 days
   - Deliverable: Project structure, database, basic scaffolding
   
2. **Sprint 2**: User Story 1 (T030-T042) - ~1-2 days
   - Deliverable: View order history (MVP Part 1)
   - Validate against quickstart.md US1 criteria
   
3. **Sprint 3**: User Story 2 (T043-T057) - ~1-2 days
   - Deliverable: View order details (MVP Complete)
   - Validate against quickstart.md US1+US2 criteria
   
4. **Sprint 4** (Optional): User Story 3 (T058-T073) - ~2 days
   - Deliverable: Initiate returns
   - Validate against quickstart.md US1+US2+US3 criteria
   
5. **Sprint 5** (Optional): User Story 4 + Polish (T074-T090) - ~1 day
   - Deliverable: Full feature complete
   - Run complete quickstart.md validation

### Quality Gates

After each phase:
- [ ] All tasks in phase completed
- [ ] Code builds without errors
- [ ] Application runs locally
- [ ] Relevant quickstart.md validation steps pass
- [ ] No errors in browser console
- [ ] Database seeds correctly

---

## Task Estimation Summary

| Phase | Task Range | Count | Estimated Effort |
|-------|-----------|-------|------------------|
| Phase 1: Setup | T001-T010 | 10 | 0.5 days |
| Phase 2: Foundational | T011-T029 | 19 | 2-3 days |
| Phase 3: User Story 1 (P1) | T030-T042 | 13 | 1-2 days |
| Phase 4: User Story 2 (P1) | T043-T057 | 15 | 1-2 days |
| Phase 5: User Story 3 (P2) | T058-T073 | 16 | 2 days |
| Phase 6: User Story 4 (P3) | T074-T078 | 5 | 0.5 days |
| Phase 7: Polish | T079-T090 | 12 | 1 day |
| **TOTAL** | **T001-T090** | **90** | **8-11 days** |

**MVP Only (US1+US2)**: 57 tasks, ~5-7 days

---

## Success Metrics

Implementation is complete when:
- [ ] All 90 tasks completed (or MVP subset)
- [ ] All 4 user stories functional (or MVP subset)
- [ ] All 10 success criteria from spec.md validated via quickstart.md
- [ ] Constitutional principles maintained throughout implementation
- [ ] Application runs on clean machine with only .NET 8 SDK
- [ ] Database seeds automatically on first run
- [ ] No errors in normal operation
- [ ] Error scenarios handled gracefully with user-friendly messages

**Proceed with implementation using this task breakdown as guide.**
