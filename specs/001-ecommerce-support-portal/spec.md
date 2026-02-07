# Feature Specification: ContosoShop E-commerce Support Portal

**Feature Branch**: `001-ecommerce-support-portal`  
**Created**: 2026-02-04  
**Updated**: 2026-02-06 (Security Enhancement + Item-Level Returns + Inventory Management)
**Status**: In Progress - Security + Inventory Features Added  
**Input**: User description: "The stakeholder documents and constitution.md file have been updated to address security concerns that weren't originally identified. I need to update the ContosoShop E-commerce Support Portal (Local Edition) - a sample web application that simulates an online store's customer support interface - to address the security concerns identified in the stakeholder documents and constitution.md file. Additionally, implement item-level returns with justification tracking and a comprehensive inventory management system with automatic reservation and restoration."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Secure User Authentication (Priority: P1) **[NEW - SECURITY CRITICAL]**

As a customer, I want to securely log in to the support portal using my credentials so that only I can access my order information.

**Why this priority**: Authentication is the foundation of all security. Without proper authentication, all other security measures are ineffective. This is a MANDATORY requirement per the updated constitution (v2.0.0) before any production deployment.

**Independent Test**: Can be tested by attempting to access the Orders page without authentication (should be redirected to login), logging in with valid credentials (should succeed), and logging in with invalid credentials (should fail with appropriate error message). Delivers critical security value by ensuring data isolation.

**Acceptance Scenarios**:

1. **Given** I am an unauthenticated user, **When** I attempt to access ANY page (home, orders, support, etc.), **Then** I am immediately redirected to the login page
2. **Given** I am on the login page with valid credentials, **When** I enter my email and password and click "Login", **Then** I am authenticated and redirected to the Orders page
3. **Given** I am on the login page with invalid credentials, **When** I enter incorrect email or password and click "Login", **Then** I see an error message "Invalid email or password" and remain on the login page
4. **Given** I have failed authentication 5 times, **When** I attempt a 6th login from the same IP address, **Then** I am rate-limited and see a message "Too many failed attempts. Please try again in 15 minutes"
5. **Given** I am authenticated, **When** my session expires after inactivity, **Then** I am automatically logged out and redirected to the login page
6. **Given** I am authenticated, **When** I manually log out, **Then** my session is terminated and I cannot access protected pages without re-authenticating

**Security Requirements**:
- MUST implement ASP.NET Core Identity or JWT-based authentication
- MUST hash passwords using industry-standard algorithms (bcrypt, PBKDF2, or Argon2)
- MUST enforce minimum password complexity (8+ characters, mix of letters/numbers/symbols)
- MUST implement session timeout (e.g., 30 minutes of inactivity)
- MUST log all authentication attempts (success and failure) with timestamps and IP addresses
- MUST rate-limit authentication attempts (5 failures per 15 minutes per IP)
- MUST use HttpOnly, Secure, and SameSite cookies for session management
- MUST validate session tokens on every API request

---

### User Story 2 - View Order History with Authorization (Priority: P1)

As an authenticated customer, I want to view a list of MY OWN past orders (and only mine) so I can quickly find and check the status of orders I've placed, with confidence that other users cannot see my orders.

**Why this priority**: This is the foundation of the support portal with mandatory authorization enforcement. Without being able to see their orders securely, customers cannot perform any other self-service actions. Authorization checks ensure data isolation per constitution requirements.

**Independent Test**: Can be fully tested by authenticating as Mateo Gomez (mateo@contoso.com) and verifying they only see their own 10 orders, then authenticating as Megan Bowen (megan@contoso.com) and verifying they see their different 10 orders. Attempting to access Mateo's order by ID while authenticated as Megan should return 403 Forbidden. Delivers immediate value with security guarantees.

**Acceptance Scenarios**:

1. **Given** I am authenticated as Mateo Gomez with 10 past orders, **When** I navigate to the Orders page, **Then** I see all 10 of MY orders listed with order number, date, total amount, and current status
2. **Given** I am authenticated as Megan Bowen with 10 past orders, **When** I navigate to the Orders page, **Then** I see ONLY my 10 orders and NONE of Mateo's orders
3. **Given** I am a new authenticated customer with no orders, **When** I navigate to the Orders page, **Then** I see a message indicating "No orders found"
4. **Given** I am viewing my order list, **When** I look at each order, **Then** I can clearly distinguish between different statuses (Processing, Shipped, Delivered, Returned) through visual indicators
5. **Given** I am authenticated, **When** the API request to GET /api/orders is made, **Then** the Authorization header contains my valid authentication token
6. **Given** I attempt to access /api/orders without authentication, **When** the API processes my request, **Then** I receive HTTP 401 Unauthorized

**Security Requirements**:
- MUST use `[Authorize]` attribute on OrdersController
- MUST filter orders by authenticated user ID: `context.Orders.Where(o => o.UserId == currentUserId)`
- MUST validate authentication token on every request
- MUST return 401 Unauthorized if token is missing or invalid
- MUST log order access attempts for audit trails

---

### User Story 3 - View Order Details with Authorization (Priority: P1)

As an authenticated customer, I want to view detailed information about MY OWN specific orders including items purchased, quantities, prices, and delivery timeline so I can verify what I ordered and track its progress, with assurance that I cannot view other users' orders.

**Why this priority**: This complements User Story 2 and is essential for the MVP with mandatory authorization. Customers need detailed order information securely to make informed decisions about returns or support requests.

**Independent Test**: Can be tested independently by authenticating as Mateo Gomez, selecting their order, and verifying the details page shows complete information. Attempting to access Megan's order ID via URL manipulation while authenticated as Mateo should return 403 Forbidden. Delivers value with authorization guarantees.

**Acceptance Scenarios**:

1. **Given** I am authenticated and have selected MY order from my order list, **When** I view the order details, **Then** I see all items in the order with product names, quantities, and prices
2. **Given** I am authenticated as Mateo Gomez and attempt to view Megan Bowen's order by ID, **When** the system processes my request, **Then** I receive HTTP 403 Forbidden with message "You do not have permission to view this order"
3. **Given** I am viewing order details for my delivered order, **When** I check the timeline, **Then** I see the order date, ship date, and delivery date
4. **Given** I am viewing order details for my shipped order, **When** I check the status, **Then** I see current status as "Shipped" with relevant information
5. **Given** I attempt to view an order that doesn't exist, **When** the system processes my request, **Then** I receive HTTP 404 Not Found
6. **Given** I attempt to access /api/orders/{id} without authentication, **When** the API processes my request, **Then** I receive HTTP 401 Unauthorized

**Security Requirements**:
- MUST use `[Authorize]` attribute on OrdersController.GetOrder(id) endpoint
- MUST verify order ownership: `order.UserId == currentAuthenticatedUserId`
- MUST return 403 Forbidden if order belongs to different user
- MUST return 404 Not Found if order doesn't exist (don't leak existence to unauthorized users)
- MUST validate authentication token on every request
- MUST log order detail access for audit trails with user ID and order ID

---

### User Story 4 - Initiate Item-Level Returns with CSRF Protection (Priority: P2)

As an authenticated customer, I want to securely initiate returns for individual items (or partial quantities) from MY delivered orders so I can receive refunds for specific items I'm not satisfied with, with protection against cross-site request forgery attacks.

**Why this priority**: This is a key self-service feature that reduces support burden and provides granular control over returns. Item-level returns enable customers to return only the items they're unsatisfied with while keeping others. CSRF protection is MANDATORY per constitution for all state-changing operations.

**Independent Test**: Can be tested by authenticating, navigating to a delivered order with multiple items, selecting one item to return with a specified quantity and reason, and verifying the return is recorded with appropriate refund amount. Attempting to POST return request without CSRF token should fail with 400 Bad Request. Attempting to return more than the available quantity should fail with validation error. Delivers value with security guarantees and business logic validation.

**Acceptance Scenarios**:

1. **Given** I am authenticated and viewing details of MY delivered order with multiple items, **When** I see the order details page, **Then** each item displays a "Return Item" button with the remaining returnable quantity shown (e.g., "Return Item (2 available)")
2. **Given** I am authenticated and click "Return Item" on a specific order item, **When** the return form appears, **Then** I see:
   - Quantity selector (1 to remaining returnable quantity)
   - Reason text area (required, max 500 characters)
   - Calculated refund amount (item price × selected quantity)
   - Submit button with embedded CSRF token
3. **Given** I submit a valid return request for 2 units of an item with reason "Defective", **When** the system processes my request with valid CSRF token, **Then**:
   - The OrderItem's ReturnedQuantity increases by 2
   - An OrderItemReturn record is created with quantity=2, reason="Defective", timestamp, and refund amount
   - Inventory is restored (2 units change from Reserved to In Stock with return history flag)
   - I see confirmation: "Your refund will be processed within 3 business days following receipt of the returned item(s)"
   - The remaining returnable quantity updates (e.g., from "Return Item (3 available)" to "Return Item (1 available)")
4. **Given** I previously returned 2 units of an item and 1 unit remains, **When** I view the order details, **Then** I see:
   - Return transaction history showing previous return (date, quantity: 2, reason: "Defective", refund amount)
   - "Return Item (1 available)" button for the remaining unit
5. **Given** I attempt to return more units than available (e.g., 5 when only 3 are available), **When** I submit the request, **Then** I receive validation error: "Cannot return 5 units - only 3 available for return"
6. **Given** I attempt to return an item without providing a reason, **When** I submit the request, **Then** I receive validation error: "Return reason is required"
7. **Given** I attempt to return an item from an order belonging to another user, **When** the system processes my request, **Then** I receive HTTP 403 Forbidden
8. **Given** an attacker attempts to forge a return request from a malicious website, **When** the system receives the request without valid CSRF token, **Then** the request is rejected with HTTP 400 Bad Request
9. **Given** I have returned all units of all items in an order, **When** I view the order details, **Then** no "Return Item" buttons are displayed and each item shows "Fully Returned"
10. **Given** I am viewing MY order that is not delivered (Processing or Shipped), **When** I check for return options, **Then** no "Return Item" buttons are displayed
11. **Given** I attempt to return items more than 10 times in an hour, **When** the system receives the 11th request, **Then** I am rate-limited with HTTP 429 Too Many Requests

**Security Requirements**:
- MUST use `[Authorize]` attribute on OrdersController.ProcessItemReturnAsync(orderId, itemId) endpoint
- MUST validate anti-forgery token via `[ValidateAntiForgeryToken]` or equivalent middleware
- MUST verify order ownership: `order.UserId == currentAuthenticatedUserId`
- MUST return 403 Forbidden if order belongs to different user
- MUST return 400 Bad Request if CSRF token is missing or invalid
- MUST validate order status is "Delivered" before processing
- MUST validate return quantity ≤ (original quantity - already returned quantity)
- MUST validate return reason is provided and ≤ 500 characters
- MUST implement rate limiting: 10 requests per hour per user for return endpoint
- MUST return 429 Too Many Requests if rate limit exceeded
- MUST log return action with: user ID, order ID, item ID, quantity, timestamp, IP address (NO financial amounts, NO PII in logs)
- MUST sanitize logs to exclude PII and detailed financial information

**Business Logic Requirements**:
- Return requests MUST create OrderItemReturn records (not update order status to "Returned")
- OrderItem.ReturnedQuantity MUST be incremented by the return quantity
- Refund amount MUST be calculated as: OrderItem.Price × return quantity
- Inventory restoration MUST be triggered automatically via InventoryService.ReturnToInventoryAsync()
- Multiple return transactions per item MUST be supported (partial returns over time)
- Return transaction history MUST be preserved and displayed to users

---

### User Story 5 - View Inventory Management (Priority: P2) **[NEW FEATURE]**

As an authenticated administrator or customer service representative, I want to view real-time inventory levels, product information, and stock status so I can answer customer inquiries about product availability and manage inventory effectively.

**Why this priority**: Inventory visibility is critical for customer service operations and order fulfillment. While not customer-facing in MVP, it provides essential operational visibility into stock levels and the relationship between orders, returns, and inventory. This feature demonstrates the full order-to-inventory lifecycle.

**Independent Test**: Can be tested by authenticating, navigating to the Inventory page, and verifying that all 25 products are displayed with correct stock counts (2,500 total items, with appropriate counts for Available, Reserved, and Returned). Creating a new order or processing a return should immediately reflect in the inventory counts on page refresh. Delivers operational value with real-time inventory visibility.

**Acceptance Scenarios**:

1. **Given** I am authenticated and navigate to the Inventory page, **When** the page loads, **Then** I see a summary showing:
   - Total Products: 25
   - Total Inventory Items: 2,500
   - Available Stock: count of items with Status = "In Stock"
   - Reserved Stock: count of items with Status = "Reserved"
   - Items with Return History: count of items with HasReturnHistory = true
2. **Given** I am viewing the Inventory page, **When** I scroll to the product table, **Then** I see each product with:
   - Item Number (e.g., ITM-001)
   - Product Name
   - Price (formatted as currency)
   - Weight (in pounds)
   - Size badge (Small/Medium/Large)
   - Total Inventory count (always 100 for each product in seed data)
   - Available Stock (green badge)
   - Reserved Stock (yellow badge if > 0, gray text if 0)
   - Returned Items count (blue badge if > 0, gray text if 0)
   - Stock Status: "In Stock" (green) if Available >= 20, "Low Stock" (yellow) if 1-19, "Out of Stock" (red) if 0
3. **Given** an order exists with status Processing/Shipped/Delivered, **When** I view the Inventory page, **Then** the Reserved Stock count reflects the quantity of items reserved for that order
4. **Given** a customer returns an item from an order, **When** I refresh the Inventory page, **Then**:
   - Available Stock increases by the return quantity
   - Reserved Stock decreases by the return quantity
   - Items with Return History count increases by the return quantity
5. **Given** I want to identify products needing reorder, **When** I scan the Stock Status column, **Then** products with "Low Stock" or "Out of Stock" are clearly highlighted with yellow or red badges
6. **Given** I attempt to access the Inventory page without authentication, **When** the system checks authorization, **Then** I am redirected to the login page

**Technical Requirements**:
- MUST use `[Authorize]` attribute on InventoryController.GetInventory() endpoint
- GET /api/inventory endpoint MUST return List<InventorySummary> with:
  - ProductId, ItemNumber, Name, Price, Weight, Dimensions
  - TotalInventory (count of all InventoryItems for this product)
  - AvailableStock (count where Status = "In Stock")
  - ReservedStock (count where Status = "Reserved")
  - ReturnedItems (count where HasReturnHistory = true)
- Inventory counts MUST be calculated in real-time from database (no denormalized/cached counts)
- Inventory page MUST use proper authentication checks and redirect if not authenticated
- Stock status thresholds: In Stock (>= 20), Low Stock (1-19), Out of Stock (0)

**Data Model Requirements**:
- Product entity with 25 seeded products (ItemNumber ITM-001 to ITM-025)
- InventoryItem entity with 2,500 items (100 per product)
- Serial number format: ITM-XXX-YYYY (e.g., ITM-001-0042)
- Status values: "In Stock", "Reserved"
- HasReturnHistory boolean flag for audit trail
- Automatic inventory reservation via InventoryService.ReserveInventoryForOrderAsync()
- Automatic inventory restoration via InventoryService.ReturnToInventoryAsync()
- FIFO (First In, First Out) logic for inventory allocation

---

### User Story 6 - Access Support Resources (Priority: P3)

As an authenticated customer, I want to access a Contact Support page so I can find information about how to reach customer service for issues that cannot be resolved through self-service.

**Why this priority**: This provides a fallback for authenticated customers but is lower priority since it's initially a static information page. It sets the foundation for future AI-powered support enhancements.

**Independent Test**: Can be tested by authenticating, navigating to the Contact Support page, and verifying that contact information is displayed clearly. Delivers value by providing customers with support options.

**Acceptance Scenarios**:

1. **Given** I am authenticated and need help that isn't available through self-service, **When** I navigate to the Contact Support page, **Then** I see contact information (email, phone) for customer service
2. **Given** I am on the Contact Support page, **When** I review available options, **Then** I see a placeholder or message indicating that interactive chat support will be available in the future

---

### Edge Cases & Security Scenarios

- **Authentication Edge Cases**:
  - What happens when a user's session expires mid-operation? (User is redirected to login with return URL; operation must be re-initiated after re-authentication)
  - What happens when multiple devices are logged in with same account? (All sessions are valid; logout on one device doesn't affect others unless global session invalidation is implemented)
  - What happens during password reset? (Out of scope for MVP; production would implement secure password reset flow)

- **Authorization Edge Cases**:
  - What happens when a customer attempts to return an item from an order that was placed more than 30 days ago? (System allows all delivered orders for this demo version; production would enforce time limits with proper error messaging)
  - How does the system handle concurrent return requests for the same order item? (System processes requests sequentially; database row locking prevents race conditions; second request validates against updated ReturnedQuantity)
  - What happens when a user attempts to access another user's order by manipulating URLs? (Authorization check returns 403 Forbidden; attempt is logged for security monitoring)
  - What happens when a user attempts to return more items than originally ordered? (Validation check fails with error message; request rejected with 400 Bad Request)

- **Item-Level Return Edge Cases**:
  - What happens when a user attempts to return 0 quantity? (Validation fails; minimum quantity is 1)
  - What happens when a user attempts multiple partial returns of the same item? (System supports this; each return creates separate OrderItemReturn record with its own justification and timestamp)
  - What happens when all units of an item have been returned? (Return button disappears; item shows "Fully Returned" status)
  - What happens if return reason exceeds 500 characters? (Validation fails; error message displayed; request rejected with 400 Bad Request)
  - How does the system handle returning items without ProductId? (Legacy orders without ProductId skip inventory restoration; refund still processes; warning logged)

- **Inventory Edge Cases**:
  - What happens when an order is placed but insufficient inventory is available? (Warning is logged; order creation continues; inventory reservation reserves what's available)
  - What happens when inventory restoration fails during return processing? (Return transaction still completes; inventory discrepancy is logged; system continues)
  - What happens when inventory is manually adjusted outside the system? (Inventory counts reflect database state; no caching means immediate consistency)
  - What happens when viewing inventory while orders are being processed? (Real-time counts show current state; no stale data due to direct database queries)
  - How does FIFO logic handle inventory with same CreatedDate? (Secondary sort by Id ensures deterministic ordering)

- **CSRF Edge Cases**:
  - What happens if CSRF token expires during form submission? (Request fails with 400; user must refresh page to get new token)
  - What happens if an attacker attempts CSRF with stolen token? (Token is tied to user session; fails if sessions don't match)

- **Rate Limiting Edge Cases**:
  - What happens when legitimate user hits rate limit? (Clear error message with retry-after time; logged for monitoring; user can continue after limit resets)
  - What happens when rate limit is hit across multiple IPs (VPN switching)? (Rate limit per user ID as well as IP; both must be within limits)

- **General System Edge Cases**:
  - What happens when the database is unavailable? (System displays appropriate error message; does not expose database details; logs error for monitoring)
  - What happens if a customer refreshes the page during a return request? (Idempotency should be enforced via request tracking; MVP may process twice - production must handle this with idempotency keys)
  - How does the system handle orders with partial shipments? (Current version treats each order as a single shipment for inventory reservation; item-level returns allow partial returns of individual items within an order)

## Requirements *(mandatory)*

### Functional Requirements

**Authentication & Authorization (Security-Critical)**:
- **FR-001** [SECURITY]: System MUST implement ASP.NET Core Identity or JWT-based authentication before production deployment
- **FR-002** [SECURITY]: System MUST require authentication for all API endpoints that access user data using `[Authorize]` attribute
- **FR-003** [SECURITY]: System MUST redirect unauthenticated users to login page when attempting to access protected resources
- **FR-004** [SECURITY]: System MUST validate user credentials against hashed passwords stored in database
- **FR-005** [SECURITY]: System MUST hash passwords using industry-standard algorithms (bcrypt, PBKDF2, or Argon2)
- **FR-006** [SECURITY]: System MUST enforce minimum password complexity (8+ characters, mix of letters/numbers/symbols)
- **FR-007** [SECURITY]: System MUST implement session timeout (30 minutes of inactivity)
- **FR-008** [SECURITY]: System MUST use HttpOnly, Secure, and SameSite attributes for authentication cookies
- **FR-009** [SECURITY]: System MUST obtain user identity from authenticated claims via `HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)`
- **FR-010** [SECURITY]: System MUST verify order ownership by checking `order.UserId == currentAuthenticatedUserId` before returning any order data
- **FR-011** [SECURITY]: System MUST return HTTP 401 Unauthorized for unauthenticated requests to protected endpoints
- **FR-012** [SECURITY]: System MUST return HTTP 403 Forbidden when authenticated user attempts to access another user's resources
- **FR-013** [SECURITY]: System MUST log all authentication attempts (success and failure) with timestamp, user ID, and IP address

**CSRF Protection (Security-Critical)**:
- **FR-014** [SECURITY]: System MUST validate anti-forgery tokens for all state-changing operations (POST, PUT, DELETE)
- **FR-015** [SECURITY]: System MUST use `[ValidateAntiForgeryToken]` attribute or equivalent middleware on return endpoint
- **FR-016** [SECURITY]: System MUST return HTTP 400 Bad Request when CSRF token is missing or invalid
- **FR-017** [SECURITY]: System MUST generate fresh CSRF tokens for each authenticated session

**Rate Limiting (Security-Critical)**:
- **FR-018** [SECURITY]: System MUST implement rate limiting middleware (e.g., AspNetCoreRateLimit)
- **FR-019** [SECURITY]: System MUST limit authentication endpoint to 5 failed attempts per 15 minutes per IP address
- **FR-020** [SECURITY]: System MUST limit order listing endpoint to 60 requests per minute per authenticated user
- **FR-021** [SECURITY]: System MUST limit order details endpoint to 120 requests per minute per authenticated user
- **FR-022** [SECURITY]: System MUST limit return operations endpoint to 10 requests per hour per authenticated user
- **FR-023** [SECURITY]: System MUST return HTTP 429 Too Many Requests when rate limits are exceeded
- **FR-024** [SECURITY]: System MUST include Retry-After header in 429 responses

**Input Validation (Security-Critical)**:
- **FR-025** [SECURITY]: System MUST validate all user inputs on both client and server sides
- **FR-026** [SECURITY]: System MUST enforce model validation attributes ([Required], [MaxLength], [Range], [EmailAddress])
- **FR-027** [SECURITY]: System MUST return HTTP 400 Bad Request for invalid inputs with sanitized error messages
- **FR-028** [SECURITY]: System MUST use EF Core LINQ queries only (no raw SQL) to prevent SQL injection
- **FR-029** [SECURITY]: System MUST rely on Blazor's automatic HTML encoding to prevent XSS attacks

**Secure Logging (Security-Critical)**:
- **FR-030** [SECURITY]: System MUST NOT log Personally Identifiable Information (PII) in console logs
- **FR-031** [SECURITY]: System MUST mask email addresses in logs (e.g., j***@example.com or use hash)
- **FR-032** [SECURITY]: System MUST log financial amounts generically ("Refund processed" not "$59.99")
- **FR-033** [SECURITY]: System MUST log security events: authentication attempts, authorization failures, rate limit violations
- **FR-034** [SECURITY]: System MUST include timestamp, user ID (if authenticated), action, IP address in audit logs

**Security Headers (Security-Critical)**:
- **FR-035** [SECURITY]: System MUST implement security headers middleware with:
  - X-Content-Type-Options: nosniff
  - X-Frame-Options: DENY
  - X-XSS-Protection: 1; mode=block
  - Content-Security-Policy (appropriate for Blazor WASM)
  - Strict-Transport-Security (HSTS)
- **FR-036** [SECURITY]: System MUST enforce HTTPS for all communication via UseHttpsRedirection()
- **FR-037** [SECURITY]: System MUST redirect HTTP requests to HTTPS

**CORS Configuration (Security-Critical)**:
- **FR-038** [SECURITY]: System MUST explicitly whitelist CORS origins (https://localhost:5002, production domain)
- **FR-039** [SECURITY]: System MUST NOT use AllowAnyOrigin(), AllowAnyMethod(), or AllowAnyHeader()
- **FR-040** [SECURITY]: System MUST whitelist only required HTTP methods (GET, POST)
- **FR-041** [SECURITY]: System MUST whitelist only required headers (Content-Type, Authorization)

**Order Management (Functional with Security)**:
- **FR-042**: System MUST display a list of all orders for the authenticated user, showing order number, order date, total amount, and current status
- **FR-043**: System MUST filter orders by authenticated user ID to ensure data isolation
- **FR-044**: System MUST allow authenticated users to select an order from their list and view detailed information
- **FR-045**: System MUST verify order ownership before displaying order details
- **FR-046**: System MUST display order status accurately using: Processing, Shipped, Delivered, Returned
- **FR-047**: System MUST provide visual differentiation between different order statuses through color coding or icons
- **FR-048**: System MUST display order timeline information including order date, ship date, and delivery date
- **FR-049**: System MUST provide a "Return Item" action for each individual item in orders with status "Delivered" owned by authenticated user
- **FR-050**: System MUST NOT display "Return Item" actions for orders with status other than "Delivered"
- **FR-051**: System MUST display remaining returnable quantity for each item (original quantity - returned quantity)
- **FR-052**: System MUST allow users to specify return quantity (1 to remaining returnable quantity) and reason (required, max 500 characters)
- **FR-053**: System MUST calculate refund amount as: item price × return quantity
- **FR-054**: System MUST create OrderItemReturn record for each return transaction with quantity, reason, timestamp, and refund amount
- **FR-055**: System MUST increment OrderItem.ReturnedQuantity by the returned quantity
- **FR-056**: System MUST validate that return quantity ≤ (original quantity - already returned quantity)
- **FR-057**: System MUST validate that return reason is provided and ≤ 500 characters
- **FR-058**: System MUST display return transaction history showing all previous returns for each item (date, quantity, reason, refund amount)
- **FR-059**: System MUST log a sanitized refund confirmation (without PII or detailed amounts) when a return is processed
- **FR-060**: System MUST validate that return requests are only processed for delivered orders owned by authenticated user
- **FR-061**: System MUST automatically restore inventory via InventoryService.ReturnToInventoryAsync() when returns are processed
- **FR-062**: System MUST display confirmation message: "Your refund will be processed within 3 business days following receipt of the returned item(s)"

**Inventory Management (Functional with Security)**:
- **FR-063**: System MUST maintain a product catalog with 25 products (ItemNumber ITM-001 through ITM-025)
- **FR-064**: System MUST store product information: ItemNumber, Name, Price, Weight, Dimensions
- **FR-065**: System MUST maintain inventory with 2,500 serialized items (100 units per product)
- **FR-066**: System MUST generate unique serial numbers in format ITM-XXX-YYYY for each inventory item
- **FR-067**: System MUST track inventory status: "In Stock", "Reserved"
- **FR-068**: System MUST track HasReturnHistory flag for items that have been returned at least once
- **FR-069**: System MUST automatically reserve inventory when orders reach Processing/Shipped/Delivered status
- **FR-070**: System MUST use FIFO (First In, First Out) logic for inventory reservation (oldest items first by CreatedDate)
- **FR-071**: System MUST automatically restore inventory when returns are processed
- **FR-072**: System MUST use FIFO logic for inventory restoration (oldest reserved items first by LastStatusChange)
- **FR-073**: System MUST link OrderItems to Products via ProductId foreign key (nullable for backward compatibility)
- **FR-074**: System MUST use Product.Price for all new orders (no random pricing)
- **FR-075**: System MUST provide GET /api/inventory endpoint requiring authentication
- **FR-076**: System MUST calculate inventory counts in real-time from database (no cached/denormalized counts)
- **FR-077**: System MUST return InventorySummary DTOs with TotalInventory, AvailableStock, ReservedStock, ReturnedItems counts
- **FR-078**: System MUST display Inventory page with summary statistics and detailed product table
- **FR-079**: System MUST display stock status indicators: "In Stock" (≥20), "Low Stock" (1-19), "Out of Stock" (0)
- **FR-080**: System MUST use color-coded badges: green (available), yellow (reserved/low stock), blue (returned), red (out of stock)

**Error Handling & User Experience**:
- **FR-081**: System MUST handle errors gracefully and display user-friendly error messages
- **FR-082**: System MUST NOT expose system internals, stack traces, or database details in error messages
- **FR-083**: System MUST provide a Contact Support page with customer service contact information
- **FR-084**: System MUST persist all order data including orders, order items, dates, and status changes in a database
- **FR-085**: System MUST persist all return transactions in OrderItemReturn table with full audit trail
- **FR-086**: System MUST persist all inventory data in Product and InventoryItem tables
- **FR-087**: System MUST provide a responsive user interface that works on desktop and mobile browsers (320px to 1920px+)
- **FR-088**: System MUST load order list and details through authenticated API calls
- **FR-089**: System MUST return appropriate HTTP status codes: 200 (success), 400 (validation), 401 (unauthorized), 403 (forbidden), 404 (not found), 429 (rate limited), 500 (server error)
- **FR-090**: System MUST provide feedback to users during asynchronous operations (loading indicators)

**Development & Deployment**:
- **FR-091**: System MUST seed the database with two demo user accounts: Mateo Gomez (mateo@contoso.com) and Megan Bowen (megan@contoso.com), both with password Password123! (hashed), and approximately 10 sample orders per user for demonstration
- **FR-092**: System MUST seed the database with 25 products (ItemNumber ITM-001 through ITM-025) with realistic names, prices, weights, and dimensions
- **FR-093**: System MUST seed the database with 2,500 inventory items (100 units per product) with unique serial numbers
- **FR-094**: System MUST automatically reserve inventory for existing orders during database initialization
- **FR-095**: System MUST link order items to products via ProductId for new orders
- **FR-096**: System MUST use simulated email service that logs sanitized messages to console in development
- **FR-097**: System MUST use User Secrets for sensitive configuration in development
- **FR-098**: System MUST support Azure Key Vault for secrets management in production
- **FR-099**: System MUST store connection strings and API keys outside source control

### Key Entities

- **User**: Represents an authenticated customer account containing:
  - Id (int, primary key)
  - Name (string, max 100 chars, required)
  - Email (string, max 255 chars, required, unique, validated)
  - PasswordHash (string, never exposed to client)
  - Orders (navigation property, one-to-many)
  - CreatedAt (DateTime)
  
- **Order**: Represents a customer's purchase transaction, containing:
  - Id (int, primary key)
  - UserId (int, foreign key, required - enables authorization)
  - OrderDate (DateTime, required)
  - Status (OrderStatus enum: Processing/Shipped/Delivered/Returned, required)
  - TotalAmount (decimal, required)
  - ShipDate (DateTime?, nullable)
  - DeliveryDate (DateTime?, nullable)
  - Items (navigation property, one-to-many)
  - User (navigation property, many-to-one)

- **OrderItem**: Represents an individual product within an order, containing:
  - Id (int, primary key)
  - OrderId (int, foreign key, required)
  - ProductId (int?, foreign key, nullable - for backward compatibility)
  - ProductName (string, max 200 chars, required)
  - Quantity (int, required, min 1)
  - Price (decimal, required, min 0)
  - ReturnedQuantity (int, default 0 - tracks total units returned)
  - Order (navigation property, many-to-one)
  - Product (navigation property, many-to-one, nullable)
  - Returns (navigation property, one-to-many to OrderItemReturn)

- **OrderItemReturn**: Represents an individual return transaction for order items, containing:
  - Id (int, primary key)
  - OrderItemId (int, foreign key, required)
  - Quantity (int, required, min 1)
  - Reason (string, max 500 chars, required - justification for return)
  - ReturnedDate (DateTime, required - when return was processed)
  - RefundAmount (decimal, required, min 0 - calculated as item price × quantity)
  - OrderItem (navigation property, many-to-one)

- **Product**: Represents a product in the catalog, containing:
  - Id (int, primary key)
  - ItemNumber (string, max 50 chars, required, unique - e.g., ITM-001)
  - Name (string, max 200 chars, required)
  - Price (decimal, required, min 0)
  - Weight (decimal, required, min 0 - in pounds)
  - Dimensions (string, max 20 chars, required - Small/Medium/Large)
  - OrderItems (navigation property, one-to-many)
  - InventoryItems (navigation property, one-to-many)

- **InventoryItem**: Represents an individual serialized physical item in inventory, containing:
  - Id (int, primary key)
  - ProductId (int, foreign key, required)
  - SerialNumber (string, max 50 chars, required, unique - format ITM-XXX-YYYY)
  - Status (string, max 20 chars, required - "In Stock"/"Reserved")
  - HasReturnHistory (bool, default false - true if ever returned)
  - CreatedDate (DateTime, required - when item was added to inventory)
  - LastStatusChange (DateTime?, nullable - timestamp of most recent status update)
  - Product (navigation property, many-to-one)

- **OrderStatus**: Enumeration of possible order states:
  - Processing (0)
  - Shipped (1)
  - Delivered (2)
  - Returned (3)

### Non-Functional Requirements

**Security (Constitutional Requirements)**:
- **NFR-001**: System MUST comply with Constitution v2.0.0 Security-First Design principle
- **NFR-002**: System MUST enforce authentication on all data access operations
- **NFR-003**: System MUST enforce authorization on all resource access operations
- **NFR-004**: System MUST protect against OWASP Top 10 vulnerabilities:
  - Broken Access Control (via authentication & authorization)
  - Cryptographic Failures (via HTTPS, password hashing)
  - Injection (via EF Core parameterized queries)
  - Insecure Design (via security requirements in design phase)
  - Security Misconfiguration (via security headers, CORS)
  - Vulnerable Components (via dependency scanning)
  - Authentication Failures (via rate limiting, session management)
  - Data Integrity Failures (via CSRF protection)
  - Security Logging Failures (via comprehensive audit logging)
  - Server-Side Request Forgery (N/A for this application)

**Performance**:
- **NFR-005**: Authenticated users can view their order history in under 2 seconds
- **NFR-006**: Authenticated users can view order details in under 1 second
- **NFR-007**: API endpoints MUST respond within 500ms under normal conditions (excluding network latency)
- **NFR-008**: Rate limiting MUST not impact legitimate user operations

**Usability**:
- **NFR-009**: Login process MUST complete in under 10 seconds
- **NFR-010**: Error messages MUST be clear and actionable
- **NFR-011**: UI MUST be responsive on screen sizes from 320px (mobile) to 1920px+ (desktop)

**Maintainability**:
- **NFR-012**: System MUST use Dependency Injection for all services
- **NFR-013**: System MUST separate business logic from presentation and data access layers
- **NFR-014**: System MUST include XML documentation for all public APIs
- **NFR-015**: System MUST follow .NET naming conventions and C# coding standards

**Testability**:
- **NFR-016**: Authentication logic MUST be testable via mock authentication
- **NFR-017**: Authorization logic MUST be testable via user context mocking
- **NFR-018**: CSRF protection MUST be testable via token validation tests
- **NFR-019**: Rate limiting MUST be testable via load tests
- **NFR-020**: Test coverage MUST exceed 70% for business logic, 90% for security-critical code

**Portability**:
- **NFR-021**: System MUST run on Windows, Linux, and macOS with .NET 8 SDK
- **NFR-022**: System MUST support switching from SQLite to Azure SQL via configuration change only
- **NFR-023**: System MUST support multiple authentication providers via configuration

## Success Criteria *(mandatory)*

### Measurable Outcomes

**Security Validation (Critical)**:
- **SC-001**: Authentication system is functional - users can successfully log in with valid credentials and are denied with invalid credentials
- **SC-002**: Authorization is enforced - Mateo Gomez cannot access Megan Bowen's orders via any method (UI navigation, URL manipulation, API calls)
- **SC-003**: CSRF protection is active - Return requests without valid anti-forgery tokens are rejected with HTTP 400
- **SC-004**: Rate limiting is functional - Exceeding defined limits (e.g., 5 auth failures, 10 returns/hour) results in HTTP 429
- **SC-005**: Logs contain NO PII - All console logs are audited and contain only sanitized data (masked emails, no amounts, no personal data)
- **SC-006**: Security headers are present - All HTTP responses include required security headers (X-Frame-Options, CSP, HSTS, etc.)
- **SC-007**: CORS is restrictively configured - Only whitelisted origins, methods, and headers are allowed
- **SC-008**: HTTPS is enforced - HTTP requests are automatically redirected to HTTPS
- **SC-009**: Passwords are hashed - Database inspection confirms passwords are stored as hashes, not plaintext
- **SC-010**: Session management is secure - Cookies use HttpOnly, Secure, and SameSite attributes

**Functional Performance**:
- **SC-011**: Users can complete login process in under 10 seconds from entering credentials to viewing Orders page
- **SC-012**: Authenticated users can view their complete order history in under 2 seconds from clicking the Orders link
- **SC-013**: Authenticated users can view order details for any of their orders in under 1 second from selecting it
- **SC-014**: Authenticated users can complete the return process in under 30 seconds from clicking "Return Order" to seeing confirmation

**Development & Deployment**:
- **SC-015**: The application runs successfully on a local machine with only .NET 8 SDK installed
- **SC-016**: The application starts and displays the login page in under 10 seconds after running the project
- **SC-017**: Sample data (users with hashed passwords and orders) is seeded automatically on first run
- **SC-018**: No secrets are committed to source control - all sensitive configuration uses User Secrets or environment variables

**User Experience**:
- **SC-019**: The user interface is responsive and functional on screen sizes from 320px (mobile) to 1920px+ (desktop)
- **SC-020**: Error messages are clear, actionable, and do not expose system internals
- **SC-021**: All form validation provides immediate feedback to users
- **SC-022**: Loading states are clearly indicated during asynchronous operations

**Architecture Quality**:
- **SC-023**: All API endpoints that access user data use `[Authorize]` attribute
- **SC-024**: All state-changing endpoints validate anti-forgery tokens
- **SC-025**: All data access uses EF Core LINQ (no raw SQL queries exist in codebase)
- **SC-026**: The application architecture supports switching from SQLite to Azure SQL by changing connection string and provider only
- **SC-027**: Authentication provider can be changed via configuration without code refactoring

**Testing Coverage**:
- **SC-028**: Security tests verify authentication requirement on all protected endpoints
- **SC-029**: Security tests verify authorization enforcement (users can only access own data)
- **SC-030**: Security tests verify CSRF token validation on state-changing operations
- **SC-031**: Security tests verify rate limiting behavior
- **SC-032**: Unit tests achieve >70% coverage for business logic
- **SC-033**: Security-critical code achieves >90% test coverage

**Compliance**:
- **SC-034**: All code complies with Constitution v2.0.0 Security-First Design principle
- **SC-035**: All code complies with Constitution v2.0.0 API-Driven Development principle (authorization enforcement)
- **SC-036**: All code complies with Constitution v2.0.0 Cloud-Ready Design principle (secure CORS, database security)
- **SC-037**: Code review checklist confirms no constitutional violations

---

## Technical Constraints

**Mandatory Security Controls**:
- ASP.NET Core Identity or JWT authentication (implementation choice documented in plan.md)
- `[Authorize]` attribute on all protected endpoints
- `[ValidateAntiForgeryToken]` or middleware on all POST/PUT/DELETE endpoints
- Rate limiting middleware (AspNetCoreRateLimit or equivalent)
- Security headers middleware (NWebsec or custom)
- HTTPS redirection and HSTS
- EF Core LINQ only (no raw SQL)
- Blazor automatic HTML encoding (no unescaped content)

**Development Tools**:
- .NET 8 SDK (minimum required version)
- Visual Studio 2022 or VS Code with C# extension
- Entity Framework Core 8.x
- SQLite (development) / Azure SQL (production)

**Framework Choices**:
- Backend: ASP.NET Core 8 Web API
- Frontend: Blazor WebAssembly
- ORM: Entity Framework Core 8
- Authentication: ASP.NET Core Identity or JWT (TBD in plan.md)
- Logging: Microsoft.Extensions.Logging with PII sanitization

**Configuration Management**:
- Development: User Secrets for sensitive config
- Production: Azure Key Vault with Managed Identity
- Connection strings externalized via appsettings.json
- Environment-specific overrides supported

---

## Out of Scope

The following are explicitly **OUT OF SCOPE** for this feature:

**Authentication Related**:
- Multi-factor authentication (MFA) - future enhancement
- Password reset workflow - future enhancement
- Social login (Google, Facebook, etc.) - future enhancement
- Remember me functionality - future enhancement

**Authorization Related**:
- Role-based access control (RBAC) - admin roles future enhancement
- Permission-based authorization - future enhancement

**Payment Integration**:
- Real payment gateway integration - simulated only
- Actual refund processing - status change only
- Payment card storage - out of scope

**Advanced Features**:
- Email notifications via actual SMTP/SendGrid - simulated logging only in MVP
- SMS notifications - out of scope
- Real-time order tracking - out of scope
- AI-powered support chat - placeholder only, future enhancement

**Operational**:
- Production deployment automation - manual deployment acceptable
- Monitoring dashboards - basic logging only
- Backup and disaster recovery procedures - not documented
- Load testing and performance optimization - basic performance only

**Data Management**:
- Data retention policies - not implemented
- GDPR right to erasure automation - manual process acceptable
- Data export functionality - out of scope

**User Management**:
- User registration workflow - demo users seeded only
- Profile management - basic user entity only
- Address book - out of scope

---

## Dependencies

**External Libraries**:
- Microsoft.AspNetCore.Components.WebAssembly (Blazor WASM)
- Microsoft.EntityFrameworkCore.Sqlite (development database)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore or JWT libraries (authentication)
- AspNetCoreRateLimit or equivalent (rate limiting)
- NWebsec or custom middleware (security headers)

**Development Dependencies**:
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- SQLite tools for database inspection

**Runtime Dependencies**:
- .NET 8 Runtime
- SQLite (local) or Azure SQL Database (production)
- HTTPS certificate (development cert or production cert)

**Configuration Dependencies**:
- User Secrets configured for development
- Azure Key Vault configured for production
- Environment variables for deployment

---

## Acceptance Checklist

Before this feature can be considered complete, the following MUST be verified:

### Security Acceptance (CRITICAL)
- [ ] Authentication system is fully implemented and functional
- [ ] All protected API endpoints have `[Authorize]` attribute
- [ ] Authorization checks verify user ownership of resources
- [ ] CSRF protection is implemented and validated on all state-changing operations
- [ ] Rate limiting is configured and functional for all specified endpoints
- [ ] Security headers are present in all HTTP responses
- [ ] CORS is configured with explicit whitelists (no wildcards)
- [ ] HTTPS is enforced and HTTP redirects to HTTPS
- [ ] Passwords are hashed using approved algorithms
- [ ] Session cookies use HttpOnly, Secure, and SameSite attributes
- [ ] All logs are audited and contain NO PII
- [ ] No secrets are present in source control

### Functional Acceptance
- [ ] User can successfully log in with valid credentials
- [ ] User cannot log in with invalid credentials
- [ ] User can view list of their own orders only
- [ ] User cannot access other users' orders (403 Forbidden)
- [ ] User can view details of their own orders
- [ ] User can initiate return for their own delivered orders
- [ ] User cannot initiate return for other users' orders
- [ ] Order status updates correctly when return is processed
- [ ] Contact Support page displays correctly

### Testing Acceptance
- [ ] Security tests cover authentication enforcement
- [ ] Security tests cover authorization enforcement
- [ ] Security tests cover CSRF validation
- [ ] Security tests cover rate limiting
- [ ] Unit tests achieve >70% coverage for business logic
- [ ] Security-critical code achieves >90% test coverage
- [ ] All tests pass successfully

### Code Quality Acceptance
- [ ] Code follows .NET naming conventions
- [ ] All public APIs have XML documentation
- [ ] No raw SQL queries exist in codebase
- [ ] Dependency Injection used throughout
- [ ] Constitution v2.0.0 compliance verified
- [ ] Code review completed and approved

### Documentation Acceptance
- [ ] README updated with authentication setup instructions
- [ ] Demo user credentials documented
- [ ] Security configuration documented
- [ ] Rate limiting configuration documented
- [ ] Deployment requirements documented

---

## Version History

**v2.0 (2026-02-05)**: Major security enhancement update
- Added comprehensive authentication and authorization requirements
- Added CSRF protection requirements
- Added rate limiting requirements
- Added secure logging requirements (PII sanitization)
- Added security headers requirements
- Enhanced all user stories with security acceptance criteria
- Updated requirements section with 66 functional requirements (many security-focused)
- Added security-focused success criteria and acceptance checklist
- Updated to align with Constitution v2.0.0

**v1.0 (2026-02-04)**: Initial specification
- Defined 4 core user stories
- Established functional requirements
- Defined key entities
- Set baseline success criteria
