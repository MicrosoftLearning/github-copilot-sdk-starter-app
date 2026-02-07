# QuickStart Guide: ContosoShop E-commerce Support Portal

**Feature**: 001-ecommerce-support-portal  
**Date**: 2026-02-04  
**Purpose**: Verify implementation against specification requirements

## Prerequisites

Before running validation tests, ensure:

- [ ] .NET 8 SDK installed (`dotnet --version` shows 8.0.x)
- [ ] Solution builds without errors (`dotnet build`)
- [ ] Database migrations applied or EnsureCreated() runs on startup
- [ ] Sample data seeded (demo user + sample orders)

## Quick Validation Checklist

### ✅ User Story 1: View Order History (P1)

**Goal**: Customer can view list of all past orders

**Steps**:
1. Run the application (`dotnet run --project ContosoShop.Server` or F5 in Visual Studio)
2. Navigate to `https://localhost:5002` (or appropriate Blazor client URL)
3. Click "Orders" link in navigation menu
4. Verify order list displays

**Success Criteria**:
- [ ] Orders page loads within 2 seconds (SC-001)
- [ ] All orders shown with: order number, date, total, status
- [ ] Different statuses visually distinguished (colors/icons)
- [ ] If no orders exist, "No orders found" message displays
- [ ] Orders sorted by date (most recent first)

**Expected Data** (from seed):
- Order #1001 - Delivered - $59.99
- Order #1002 - Shipped - $15.00
- Order #1003 - Processing - $129.99
- Order #1004 - Returned - $45.00

---

### ✅ User Story 2: View Order Details (P1)

**Goal**: Customer can view detailed order information

**Steps**:
1. From Orders page, click on Order #1001
2. Verify order details page displays

**Success Criteria**:
- [ ] Order details load within 1 second (SC-002)
- [ ] All items shown: product name, quantity, price
- [ ] Order timeline shows: order date, ship date, delivery date
- [ ] Status clearly indicated
- [ ] Total amount matches sum of items
- [ ] For Order #1001: Shows 2 items (Wireless Mouse $25, Keyboard $34.99)

**Test Edge Case**:
- Navigate to non-existent order (e.g., /orders/9999)
- [ ] Displays error message "Order not found"

---

### ✅ User Story 3: Initiate Order Return (P2)

**Goal**: Customer can return delivered orders

**Steps**:
1. Navigate to Order #1001 (Delivered status)
2. Verify "Return Order" button is visible and enabled
3. Click "Return Order" button
4. Confirm return initiation

**Success Criteria**:
- [ ] Return completes within 30 seconds (SC-003)
- [ ] Order status updates to "Returned"
- [ ] Confirmation message displays
- [ ] Console shows refund email log
- [ ] "Return Order" button no longer appears

**Test Business Rules**:
- Navigate to Order #1002 (Shipped)
  - [ ] No "Return Order" button displayed
- Navigate to Order #1003 (Processing)
  - [ ] No "Return Order" button displayed
- Navigate to Order #1004 (Already Returned)
  - [ ] No "Return Order" button displayed
  - [ ] Status shows "Returned"

---

### ✅ User Story 4: Access Support Resources (P3)

**Goal**: Customer can find support contact information

**Steps**:
1. Click "Contact Support" link in navigation
2. Verify support page displays

**Success Criteria**:
- [ ] Contact information visible (email, phone)
- [ ] Placeholder message about future AI chat support
- [ ] Page loads quickly and displays properly

---

## API Endpoint Validation

### GET /api/orders

**Test with cURL** (PowerShell):
```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/orders" -Method GET
$response | ConvertTo-Json -Depth 10
```

**Expected Response**:
```json
[
  {
    "id": 1001,
    "orderDate": "2026-01-05T10:30:00Z",
    "status": "Delivered",
    "totalAmount": 59.99,
    "itemCount": 2
  },
  ...
]
```

**Validation**:
- [ ] Returns 200 OK status
- [ ] Response is valid JSON array
- [ ] Each order has id, orderDate, status, totalAmount, itemCount
- [ ] Response time < 500ms (SC-007)

---

### GET /api/orders/1001

**Test with cURL** (PowerShell):
```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/orders/1001" -Method GET
$response | ConvertTo-Json -Depth 10
```

**Expected Response**:
```json
{
  "id": 1001,
  "userId": 1,
  "orderDate": "2026-01-05T10:30:00Z",
  "status": "Delivered",
  "totalAmount": 59.99,
  "shipDate": "2026-01-06T08:00:00Z",
  "deliveryDate": "2026-01-12T15:30:00Z",
  "items": [
    {
      "id": 1,
      "productName": "Wireless Mouse",
      "quantity": 1,
      "price": 25.00
    },
    {
      "id": 2,
      "productName": "Keyboard",
      "quantity": 1,
      "price": 34.99
    }
  ]
}
```

**Validation**:
- [ ] Returns 200 OK status
- [ ] Response includes all order fields
- [ ] Items array contains all order items
- [ ] Response time < 500ms (SC-007)

**Error Case** (order not found):
```powershell
try {
    Invoke-RestMethod -Uri "https://localhost:5001/api/orders/9999" -Method GET
} catch {
    $_.Exception.Response.StatusCode  # Should be 404
}
```

---

### POST /api/orders/1001/return

**Test with cURL** (PowerShell):
```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/orders/1001/return" -Method POST
$response  # Should be empty (204 No Content)
```

**Validation**:
- [ ] Returns 204 No Content status
- [ ] Order status changed to "Returned" (verify with GET)
- [ ] Console shows refund email log message

**Error Case** (order not delivered):
```powershell
try {
    Invoke-RestMethod -Uri "https://localhost:5001/api/orders/1002/return" -Method POST
} catch {
    $_.Exception.Response.StatusCode  # Should be 400
    # Error message should explain order not eligible for return
}
```

---

## Non-Functional Requirements Validation

### SC-004: Single Dependency (NET 8 SDK)

**Validation**:
- [ ] Fresh clone of repository
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build`
- [ ] Run `dotnet run --project ContosoShop.Server`
- [ ] No external services required (SQL Server, email server, etc.)
- [ ] SQLite database creates automatically

---

### SC-005: Startup Time <10 Seconds

**Validation**:
1. Close application if running
2. Start timer
3. Run `dotnet run --project ContosoShop.Server`
4. Wait for "Application started" message
5. Navigate to `https://localhost:5002`
6. Wait for homepage to fully load
7. Stop timer

**Success**: Total time < 10 seconds

---

### SC-006: Responsive UI (320px - 1920px)

**Validation**:
1. Open browser DevTools (F12)
2. Toggle device emulation
3. Test these viewport sizes:
   - [ ] 320px width (iPhone SE)
   - [ ] 768px width (iPad)
   - [ ] 1024px width (Desktop)
   - [ ] 1920px width (Full HD)

**Success**: All pages display properly, navigation accessible, no horizontal scroll

---

### SC-009: User-Friendly Error Messages

**Test Scenarios**:

**Database Unavailable**:
1. Stop application
2. Delete or lock `App_Data/ContosoShop.db`
3. Start application
4. Expected: "Service temporarily unavailable" (not "SqliteException")

**Invalid Return Request**:
1. Attempt to return Order #1002 (Shipped)
2. Expected: "Only delivered orders can be returned" (not "Status validation failed")

---

### SC-010: Cloud Migration Ready

**Validation**:
1. Review `appsettings.json` - connection string externalized
2. Review `Program.cs` - `UseSqlite()` called on `DbContext`
3. Verify switching to Azure SQL requires only:
   - Change connection string
   - Change `UseSqlite()` to `UseSqlServer()`
   - No business logic changes

**Test** (optional if SQL Server available):
```csharp
// Change in Program.cs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ContosoContext>(options =>
    options.UseSqlServer(connectionString));  // Changed from UseSqlite

// Update connection string in appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ContosoShop;Trusted_Connection=True;"
}
```

Apply migrations: `dotnet ef database update`  
Run application - should work identically

---

## Integration Test Validation

**Run automated tests**:
```bash
dotnet test ContosoShop.Tests
```

**Expected Results**:
- [ ] All contract tests pass (OrdersApiTests)
- [ ] All integration tests pass
- [ ] Test coverage > 70% for business logic (if measured)

**Key Test Classes**:
- `OrdersApiTests.cs` - API contract validation
- `OrderServiceTests.cs` - Business logic validation
- `ReturnEligibilityTests.cs` - Return rules validation

---

## Performance Benchmarking

**Order List Load Time**:
```powershell
Measure-Command {
    Invoke-RestMethod -Uri "https://localhost:5001/api/orders"
}
```
**Expected**: < 500ms (local), < 2 seconds (UI load per SC-001)

**Order Details Load Time**:
```powershell
Measure-Command {
    Invoke-RestMethod -Uri "https://localhost:5001/api/orders/1001"
}
```
**Expected**: < 500ms (local), < 1 second (UI load per SC-002)

---

## Console Log Verification

**Expected Logs**:

**Application Startup**:
```
info: ContosoShop.Server.Data.DbInitializer[0]
      Database initialized with sample data
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Order Return Operation**:
```
info: ContosoShop.Server.Services.EmailServiceDev[0]
      EMAIL: To=john.doe@example.com, Subject=Return Confirmation, Body=Your return for Order #1001 has been processed...
info: ContosoShop.Server.Controllers.OrdersController[0]
      Return processed successfully for Order 1001
```

**Error Scenarios**:
```
warn: ContosoShop.Server.Controllers.OrdersController[0]
      Return rejected for Order 1002: Order not delivered
```

---

## Troubleshooting

### Application Won't Start

**Issue**: Port already in use  
**Solution**: 
```powershell
# Find process using port 5001
netstat -ano | findstr :5001
# Kill process
taskkill /PID <process_id> /F
```

**Issue**: Database locked  
**Solution**: 
- Close all connections to SQLite file
- Delete `App_Data/ContosoShop.db` and restart (will reseed)

---

### Orders Not Displaying

**Check**:
1. Database file exists: `App_Data/ContosoShop.db`
2. Database seeded: Check console for "Database initialized" message
3. API responds: Test `https://localhost:5001/api/orders` directly
4. Browser console: Check for JavaScript errors (F12)

---

### Return Operation Fails

**Check**:
1. Order status is "Delivered"
2. API endpoint returns 204 (not 400 or 404)
3. Console shows email log message
4. Database updated: Query Order table for status change

---

## Success Criteria Summary

| Criteria | Target | Validation Method |
|----------|--------|-------------------|
| SC-001 | Order list < 2s | Manual timing + stopwatch |
| SC-002 | Order details < 1s | Manual timing + stopwatch |
| SC-003 | Return operation < 30s | Manual timing + stopwatch |
| SC-004 | Only .NET 8 SDK needed | Fresh install test |
| SC-005 | Startup < 10s | Manual timing from launch to ready |
| SC-006 | Responsive 320-1920px | Browser DevTools testing |
| SC-007 | API response < 500ms | Measure-Command in PowerShell |
| SC-008 | Auto-seed on first run | Verify database populated automatically |
| SC-009 | User-friendly errors | Test error scenarios, check messages |
| SC-010 | Cloud migration ready | Code review, test provider switch |

---

## Final Checklist

Implementation is complete when:

- [ ] All 4 user stories validated successfully
- [ ] All 3 API endpoints respond correctly
- [ ] All 10 success criteria met
- [ ] All automated tests pass
- [ ] No errors in console during normal operation
- [ ] Error scenarios handled gracefully
- [ ] Application runs on clean machine with only .NET 8 SDK

**Sign-off**: Implementation matches specification ✅
