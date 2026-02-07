# ContosoShop E-commerce Support Portal – Feature Description

This document describes the functional features of the ContosoShop E-commerce Support Portal. The application is a simplified customer-facing support website for an online retailer, focused on allowing a user to manage orders and get post-purchase support. Below is a breakdown of the key features, user workflows, and how they operate in the base application (before adding the AI agent at some point in the future).

## 1. User Authentication and Authorization

**SECURITY REQUIREMENT: The application MUST implement proper authentication and authorization.**

**Authentication Implementation:**
- The application MUST implement ASP.NET Core Identity or JWT-based authentication
- Users MUST be required to log in before accessing any order data or performing actions
- Authentication tokens MUST be used to identify the current user in all API requests
- Login credentials MUST be validated against the database
- Sessions MUST expire after a reasonable timeout period
- For demonstration/testing purposes, seed users (e.g., "Coralie Duperre") with email/password may be created, but authentication MUST still be enforced

**Authorization Rules:**
- All backend API endpoints MUST validate that the authenticated user has permission to access the requested resource
- Users MUST only be able to view and manage their own orders (implement ownership checks)
- The backend MUST verify `order.UserId == currentAuthenticatedUserId` before returning any order data
- Return/refund operations MUST verify the order belongs to the authenticated user
- Unauthorized access attempts MUST return HTTP 403 Forbidden responses
- The API MUST use `[Authorize]` attributes on all controllers that handle sensitive data

**User Identity Management:**
- User identity MUST be obtained from authenticated claims/tokens (HttpContext.User)
- The application MUST NOT rely on hardcoded user IDs or assumptions about who is logged in
- Failed authentication attempts MUST be logged for security monitoring
- The system MUST support multiple user accounts with isolated data

**Security Token Requirements:**
- If using JWT: tokens MUST include user ID claims and MUST be validated on every API request
- If using cookies: they MUST be marked as HttpOnly, Secure, and SameSite to prevent XSS/CSRF attacks
- Token/session secrets MUST be stored securely (Azure Key Vault in production, user secrets in development)

## 2. Order Management Features

These features allow the user to see information about their purchases. All order-related data is stored in a database and accessed via the backend API.

**Order History Page:** The "Orders" page on the Blazor client displays a list of the user's past orders. For each order, it shows an overview: Order Number/ID, date of purchase, total amount, and current status.

- Example: Order #1001 – Placed on Jan 5, 2026 – Total: $59.99 – Status: Delivered.
- The data is fetched from the backend by calling GET /api/orders (which returns all orders for the demo user). In the base app, this API uses the user's ID to query the database (in our simplified scenario, it returns a static list of sample orders seeded for Mateo Gomez).
- If no orders exist (e.g., in a fresh database), the page will indicate that the user has no order history.

**Order Details View:** By clicking on an order in the history list, the user navigates to an Order Details page. This shows more granular information:

- Items in the order (product names, quantities, prices).
- Order timeline information: purchase date, shipment date (if shipped), delivery date (if delivered), etc.
- The current status is highlighted (e.g., "Delivered on Feb 10, 2026").
- This page calls GET /api/orders/{orderId} to fetch details for the selected order. The API returns a detailed order object including associated items. In the UI, a list of order items is displayed with their name, SKU, price, and quantity.
- If the order is still in process (not delivered yet), the page might show an estimated delivery or current shipping step (for example, "Your package is in transit – expected by Jul 20" if such info were available; our base sample keeps it simple with basic statuses).
- If the order ID requested doesn't belong to the user or doesn't exist, the base API would return an error. However, since our demo user only sees their own seed data, this situation doesn't occur in normal use.

**Order Status Indicators:** The possible order statuses in the base system include: Processing, Shipped, Delivered, Returned. Each status is assigned automatically by the system logic or via data seeding.

- **Processing:** Order placed but not yet shipped.
- **Shipped:** Order handed over to carrier, on the way.
- **Delivered:** Order delivered to the customer (eligible for return).
- **Returned:** Order was returned by the customer and refund processed.

These statuses are shown on both the Order History and Details pages for clarity. If an order is returned, it's clearly labeled as such (and items might be shown as returned).

**Data Persistence:** In the base app, order information is stored in a local SQLite database via Entity Framework Core. There are two main tables (entities):

- **Orders:** Contains fields like OrderId, UserId, OrderDate, Status, TotalAmount, etc. Possibly also a field for DeliveryDate.
- **OrderItems:** Contains individual line items for each order (OrderItemId, OrderId (foreign key), ProductName, Quantity, Price).

Each time the user requests their orders, the API queries this DB. The SQLite DB is pre-populated with a few orders for demonstration. (For example, Order #1001 might be a delivered order with two items, Order #1002 a shipped order with one item, etc.)

## 3. Return and Refund Capability

One of the major support functions of an e-commerce site is handling returns. The base application includes a simplified return/refund workflow:

**Return Eligibility & UI:** If an order's status is Delivered, the Order Details page will display a "Return Order" or "Initiate Return" button. This is the entry point for the user to request a return/refund for that order. (For orders not delivered or already returned, no such button is shown, preventing invalid actions.)

- The base app determines eligibility by checking the status field. Optionally, it could also check a timeframe (e.g., only allow returns within 30 days of delivery). In our demo, we assume all delivered orders are returnable (the training focus is AI integration, so we keep business rules simple).

**Return Process (Implementation with Security Controls):** When the user clicks "Return Order," the following secure workflow MUST be implemented:

**SECURITY REQUIREMENT: All state-changing operations MUST be protected against CSRF attacks.**

- The frontend calls POST /api/orders/{orderId}/return (this endpoint is implemented in the ASP.NET Core API). The request includes the order ID (and could include a reason for return, though our UI doesn't ask for one in the base version).
- **CSRF Protection:** The API endpoint MUST validate anti-forgery tokens to prevent Cross-Site Request Forgery attacks. If using cookie-based auth, implement `[ValidateAntiForgeryToken]` or equivalent middleware.
- **Authorization Check:** The backend API MUST verify the authenticated user owns the order being returned (check `order.UserId == authenticatedUserId`). If not, return HTTP 403 Forbidden.
- **Business Logic Validation:** The handler will verify that the order is indeed deliverable/returnable (status check, date check). If any check fails, return an appropriate error or status code.
- If valid, the API updates the Order's status to Returned in the database. It also creates a Refund record or notes that a refund is due.
- **Audit Logging:** Log the return action with user ID, order ID, timestamp, and IP address for security audit trails.
- The API then (in the base app) simulates sending a confirmation email to the customer. Rather than actually sending an email, it uses a service that logs the email content. For example, it might log: "Refund initiated for Order #1001 – amount $59.99 will be returned to your original payment method." This log simulates what an email would contain. (This design uses an EmailService interface, with a development implementation that just writes to console. Later, this can be swapped with a real email sender backed by SendGrid or SMTP without changing the controller logic – demonstrating a production-oriented design even in a local app.)
- Finally, the API responds to the client indicating success. The Blazor UI, upon success, might show a confirmation message like "Your return has been processed. You will receive a confirmation email shortly." and update the order status on the page to "Returned".

**Post-Return Behavior:** After a return, if the user checks the order list, Order #1001's status will now show as Returned. If they go into details, they'll see it marked returned (and no return button, since it's already done). Essentially, the system now treats it as a completed return. (The base app does not track refunds money movement beyond the status, but in a real system this is where integration with a payment gateway would happen.)

**Item-Level Returns:** The application supports granular, item-level return processing:
- Users can return individual items from an order rather than the entire order
- Each item in an order can be returned in multiple transactions (partial returns)
- Return quantities are tracked per item (OrderItem.ReturnedQuantity field)
- Each return transaction is recorded separately in the OrderItemReturn table with:
  - Quantity returned in this transaction
  - Reason/justification for the return (required, max 500 characters)
  - Timestamp of the return
  - Refund amount calculated for this specific return
- Users can return additional quantities of the same item over time until the full quantity is returned
- The system validates that total returned quantity never exceeds the original ordered quantity
- Return buttons are displayed per item with remaining returnable quantity shown
- Example: If 3 units of a product were ordered, the user can return 1 unit with reason "Defective", then later return 1 more unit with reason "Changed mind"

**Refund Processing & Communication:** When a return is successfully processed:
- The order item's returned quantity is incremented
- A refund record is created in the OrderItemReturn table
- The refund amount is calculated based on the original item price × quantity returned
- A refund confirmation message is displayed to the user: "Your refund will be processed within 3 business days following receipt of the returned item(s)"
- The system logs the return transaction for audit purposes (without exposing PII in logs)
- Inventory is automatically restored to available stock (see Inventory Management section below)

## 4. Inventory Management System

**FEATURE OVERVIEW:** The application includes a comprehensive inventory management system that tracks individual serialized items, manages stock levels, and automatically coordinates inventory reservations with order processing and returns.

**Product Catalog:**
- The system maintains a central product catalog with 25 distinct product types
- Each product has:
  - Unique Item Number (e.g., ITM-001 through ITM-025)
  - Product Name describing the item
  - Price (decimal, consistent across all inventory of that product)
  - Weight in pounds (decimal)
  - Dimensions/Size (Small, Medium, Large)
- Products are referenced by OrderItems via ProductId foreign key
- The catalog serves as the single source of truth for product pricing

**Inventory Tracking:**
- The system manages 2,500 individual inventory items (100 units per product type)
- Each inventory item has a unique Serial Number in format ITM-XXX-YYYY:
  - XXX = Product item number (001-025)
  - YYYY = Specific unit number (0001-0100)
  - Example: ITM-001-0042 represents the 42nd unit of product ITM-001
- Inventory items track the following states:
  - **In Stock**: Available for purchase
  - **Reserved**: Allocated to an active order (Processing, Shipped, or Delivered status)
  - **Returned**: Previously sold and returned by customer (remains "In Stock" but flagged with HasReturnHistory)
- Each inventory item records:
  - Current Status
  - CreatedDate (when item was added to inventory)
  - LastStatusChange (timestamp of most recent status update)
  - HasReturnHistory flag (true if item has ever been returned)

**Automatic Inventory Reservation:**
- When orders are created or updated to Processing/Shipped/Delivered status:
  - The system automatically queries available inventory (Status = "In Stock")
  - Reserves the required quantity using FIFO (First In, First Out) logic
  - Oldest inventory items (by CreatedDate) are reserved first
  - Inventory status changes from "In Stock" to "Reserved"
  - LastStatusChange timestamp is updated
- Database initialization automatically reserves inventory for existing orders
- Orders without sufficient available inventory will show warnings in logs

**Inventory Restoration on Returns:**
- When customers return items through the item-level return system:
  - The system identifies Reserved inventory for the returned product
  - Changes status from "Reserved" back to "In Stock" using FIFO (oldest reserved first)
  - Sets HasReturnHistory flag to true on returned items
  - Updates LastStatusChange timestamp
  - Inventory counts are immediately updated and visible in the UI
- Example: Customer returns 2 units of ITM-005 → 2 Reserved items for ITM-005 become In Stock with return history

**View Inventory Page:**
- Authenticated users can access the "View Inventory" page from the navigation menu
- The page displays:
  - **Summary Statistics**:
    - Total Products in catalog (25)
    - Total Inventory Items (2,500)
    - Available Stock (In Stock status count)
    - Reserved Stock (Reserved status count)
    - Items with Return History (HasReturnHistory = true count)
  - **Detailed Product Table** showing for each product:
    - Item Number (displayed as code)
    - Product Name
    - Price (formatted as currency)
    - Weight in pounds
    - Size badge (Small/Medium/Large)
    - Total Inventory count (all items for this product)
    - Available Stock (green badge)
    - Reserved Stock (yellow badge, if > 0)
    - Returned Items count (blue badge, if > 0)
    - Stock Status indicator:
      - "In Stock" (green) - Available >= 20 units
      - "Low Stock" (yellow) - Available 1-19 units
      - "Out of Stock" (red) - Available = 0 units
- Real-time inventory counts reflect current order and return statuses
- Data is fetched via GET /api/inventory endpoint with authorization required

**Integration with Orders:**
- OrderItem entities link to Products via nullable ProductId foreign key
  - Nullable for backward compatibility with legacy orders
  - New orders MUST have ProductId populated
- Order prices are derived from Product.Price (catalog pricing)
- When viewing order details, items show product information from catalog
- Return processing triggers inventory restoration automatically

**Business Rules & Validation:**
- Inventory reservation validates sufficient stock is available
- Return processing validates that Reserved inventory exists for the product
- FIFO logic ensures fair inventory allocation (oldest items used first)
- Serial numbers are preserved through return cycles (not regenerated)
- Return history is permanent audit trail (HasReturnHistory never resets)
- Insufficient inventory scenarios are logged with warnings

**Future Enhancements:**
- Order placement UI can call InventoryService.ReserveInventoryForOrderAsync
- Status transition: Reserved → Sold (when order completes delivery)
- Low stock alerts and automatic reorder notifications
- Inventory replenishment tracking
- Product catalog management UI for adding/editing products
- Bulk inventory operations
- Inventory reports and analytics

## 5. Customer Support Interface

The application has a section dedicated to customer support, which is where our AI integration will come into play. In the base application:

**Contact Support Page:** There is a page (likely accessible via a "Support" or "Contact Us" link) that is meant to assist the user in getting help. Currently, this page contains static content, such as:

- Support contact information (e.g., "For any issues, email support@contososhop.com or call 1-800-CONTOSO").
- Perhaps an input form or button that says "Chat with an Agent" or "Ask a question" – but it might be non-functional or placeholder in the base version. For example, a disabled text box that says "Support chat coming soon" or instructions like "Type your question below and click send." However, since we haven't wired up the backend for chat yet, clicking send might either do nothing or show a dummy response ("Thanks, we will get back to you.").
- The reason to include a stub here is to set the stage for future updates: this page is exactly where we'll embed the AI agent ata some point in the future.

**Current Limitations:** Without the AI agent, the support page cannot dynamically answer user queries. If a user wanted to know "Where is my order?" currently they would have to look at the Orders page themselves. The support page might just say "Contact us via email." In essence, the base app doesn't yet have interactive Q&A or support automation.

**Vision for Enhancement:** The design anticipates adding an interactive element. The page already has a layout conducive to a chat interface (e.g., an area where conversation could be displayed and an input box at the bottom). This was done intentionally to make the integration of the Copilot SDK agent smoother at some point in the future. In the future, when an AI support agent is integrated, this page will allow the user to ask questions in natural language and receive answers or actions (like initiating returns) from the AI agent, instead of the static info.

## 6. Error Handling and User Feedback

Even in a simple app, providing feedback for errors or important events is crucial:

**API Error Handling:** The backend APIs return proper HTTP status codes for error scenarios. For example, if a return is requested for an order that's not delivered (say status is Shipped), the API might return a 400 Bad Request with a message "Order not deliverable, cannot return yet." In the base UI, such error messages would be caught and displayed to the user, possibly as a notification or modal. (The current UI has a basic mechanism for showing an error alert if an API call fails – this uses Blazor's error boundary or a simple try/catch around the API call followed by showing a message in the page.)

**Confirmation Messages:** Conversely, when actions succeed (like a return processed), the UI immediately reflects the change (status updated) and may show a one-time confirmation message, e.g., "Return processed successfully." The base app's Order Details page, for instance, might have a banner that appears after a successful return action.

**Input Validation and Rate Limiting:**

**SECURITY REQUIREMENT: All user inputs MUST be validated on both client and server side.**

- All form inputs MUST be validated on the client side for immediate user feedback (using Blazor data annotations and validation)
- **Server-Side Validation (CRITICAL):** All inputs MUST be re-validated on the server - never trust client-side validation alone
- The ReturnOrder API MUST validate:
  - Order ID is a valid integer and exists in the database
  - Order status is eligible for return (Delivered only)
  - Order belongs to the authenticated user
  - Order is within the return window (if time-based policy exists)
- Model validation attributes (`[Required]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`) MUST be enforced by API controllers
- Invalid inputs MUST return HTTP 400 Bad Request with clear validation error messages (but without exposing system internals)

**SECURITY REQUIREMENT: Rate limiting MUST be implemented to prevent API abuse.**

- **Rate Limiting:** The API MUST implement rate limiting middleware (e.g., `AspNetCoreRateLimit`) to prevent:
  - Brute force attacks (limit failed login attempts)
  - Order ID enumeration attacks (limit GET /api/orders/{id} requests per minute)
  - Denial of Service (DoS) attacks (limit total requests per IP/user)
- Recommended limits:
  - Authentication endpoints: 5 failed attempts per 15 minutes per IP
  - Order listing: 60 requests per minute per user
  - Order details: 120 requests per minute per user
  - Return operations: 10 requests per hour per user
- Rate limit violations MUST return HTTP 429 Too Many Requests

**User Interface Layout:**

- **Title Bar:** The application displays "ContosoShop Support Portal" prominently in the title bar on the far left side. When authenticated, the user's email address appears on the far right side of the title bar, providing clear visual confirmation of who is currently logged in. The title bar uses flexbox layout with space-between positioning to maintain consistent spacing across all screen sizes.

- **Navigation Menu:** The left sidebar navigation is organized with:
  - **Primary navigation items** at the top: Home, Orders, Contact Support, and Logout button
  - **Administrative link** at the very bottom: "View Inventory" is anchored to the bottom of the navigation bar using flexbox positioning (`margin-top: auto`), making it visually distinct from the primary navigation while remaining easily accessible throughout the session
  - Authentication-aware display: Menu items automatically adjust based on login state (showing only Login when not authenticated, full menu when authenticated)

**Navigation & State:** The single-page nature of Blazor means users can navigate between pages (Orders list -> Order details -> Support, etc.) without full reloads. The app preserves necessary state (like selected order details are fetched each time or cached briefly). If the user performs an action and then goes back, the Orders list will refresh to show updated status (our base implementation simply re-calls the API on navigation, but we could optimize with caching). This approach ensures that the user always sees up-to-date info, even though it might re-fetch data (acceptable in a small app).

## 7. Roadmap for Cloud-Scale Features

While the base application is feature-complete for a demo, it leaves out some advanced features that a production system would have, which can be added later without restructuring:

**User Authentication & Profiles:** As noted, adding a robust authentication system (with identity management, password reset, multi-user support) is a logical next step. The front-end nav bar already has a placeholder for "Hello, [Username]" which in our case is fixed, but could tie into an auth system easily. Azure AD B2C or Identity Server could be integrated so each user sees only their orders. The database already associates orders with a UserId, which is how multi-tenancy would be enforced.

**Payment and Refund Integration:** The return process is simulated. In real life, upon marking an order as Returned, we'd call a Payment Gateway API to actually issue the refund to the customer's credit card or account. The code is structured so that the step of "issue refund" can be abstracted to a service class. Currently it's a stub that just logs, but one could plug in, say, Stripe or PayPal API calls in that spot.

**Inventory and Product Catalog:** Our focus was customer support, so we don't have product browsing or inventory management in this project. However, if one were to extend this into a full e-commerce app, one could add a Products API and pages for browsing items, adding to cart, placing orders, etc. The addition of those features would not conflict with what's built – the Orders and Support parts would continue to function and would benefit from more data.

**Admin Portal:** Another extension might be an admin interface for support reps to intervene. For instance, an admin could use a similar web UI to look up any customer's orders and manually process returns or answer queries. That would require authentication roles and exposing data by admin queries. The base app doesn't include this, but our API and DB design (with clear user IDs and order relationships) would allow an admin to retrieve any order by ID if authorized.

**Internationalization and Localization:** Currently all text is in English and amounts are in dollars. The app could be localized (Blazor has support for localization) to different languages and currencies. We didn't do it here to avoid complexity in the lab, but it's a consideration for production. Similarly, date and number formats are fixed in code but could be culture-sensitive.

The above features are outside the immediate scope, but it's important to note that the base app's design does not paint us into a corner; it can evolve.
