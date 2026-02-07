# API Contracts

**Feature**: 001-ecommerce-support-portal  
**Date**: 2026-02-04  
**Phase**: 1 - Design

## Overview

This directory contains OpenAPI (Swagger) specifications for the ContosoShop E-commerce Support Portal REST API. These contracts define the interface between the Blazor WebAssembly frontend and the ASP.NET Core Web API backend.

## Files

- **[orders-api.yaml](orders-api.yaml)** - OpenAPI 3.0 specification for Orders API endpoints

## Endpoints Summary

### Orders API

| Method | Endpoint | Purpose | User Story |
|--------|----------|---------|------------|
| GET | `/api/orders` | List all orders for user | US1 - View Order History |
| GET | `/api/orders/{id}` | Get order details | US2 - View Order Details |
| POST | `/api/orders/{id}/return` | Initiate order return | US3 - Initiate Order Return |

## Data Models

### Request Models

**POST /api/orders/{id}/return**
- No request body required
- Order ID provided in URL path
- Current user determined from authentication context (simplified in MVP)

### Response Models

**OrderSummary** (for list view)
```json
{
  "id": 1001,
  "orderDate": "2026-01-05T10:30:00Z",
  "status": "Delivered",
  "totalAmount": 59.99,
  "itemCount": 2
}
```

**OrderDetails** (for detail view)
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

**OrderItem** (embedded in OrderDetails)
```json
{
  "id": 1,
  "productName": "Wireless Mouse",
  "quantity": 1,
  "price": 25.00
}
```

**ProblemDetails** (for errors - RFC 7807)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Order 1002 cannot be returned. Only delivered orders are eligible for return."
}
```

## Status Codes

All endpoints follow standard HTTP status code conventions:

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful GET request with response body |
| 204 | No Content | Successful POST request with no response body |
| 400 | Bad Request | Invalid request (e.g., returning non-delivered order) |
| 404 | Not Found | Order doesn't exist or doesn't belong to user |
| 500 | Internal Server Error | Unexpected server-side error |

## Error Handling

All errors return RFC 7807 ProblemDetails format:

**Example: Order Not Found**
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Order with ID 1001 not found or does not belong to the current user"
}
```

**Example: Cannot Return Order**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Order 1002 cannot be returned. Only delivered orders are eligible for return."
}
```

## Content Negotiation

- **Request**: No body required (GET, POST with URL parameters)
- **Response**: `application/json` (default for ASP.NET Core API controllers)
- **Character Encoding**: UTF-8

## Versioning Strategy

**Current Version**: 1.0.0 (no version in URL)

For MVP, no API versioning is implemented. Future versions can use:
- URL path versioning: `/api/v2/orders`
- Header versioning: `Accept: application/vnd.contososhop.v2+json`
- Query parameter versioning: `/api/orders?api-version=2.0`

**Recommendation**: Use URL path versioning for simplicity when breaking changes are needed.

## Authentication & Authorization

**MVP Approach**: Simplified single-user demo mode
- No authentication required for MVP
- User context hardcoded to demo user (Id=1, "John Doe")
- All API endpoints assume authenticated user

**Production Approach**: Azure AD B2C or ASP.NET Core Identity
- Require JWT Bearer token in Authorization header
- Extract user ID from token claims
- Validate user ownership of resources

**Example Future Auth Header**:
```http
GET /api/orders HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Contract Testing

Contract tests should verify:

1. **Response Structure**: JSON matches defined schema
2. **Status Codes**: Correct codes returned for scenarios
3. **Business Rules**: Order return eligibility enforced
4. **Error Format**: Errors follow ProblemDetails format

**Example xUnit Contract Test**:
```csharp
[Fact]
public async Task GetOrders_ReturnsOrderList()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/orders");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var orders = await response.Content.ReadFromJsonAsync<List<OrderSummary>>();
    Assert.NotNull(orders);
    Assert.All(orders, o => {
        Assert.True(o.Id > 0);
        Assert.True(o.TotalAmount >= 0);
        Assert.NotNull(o.Status);
    });
}
```

## OpenAPI/Swagger Integration

To enable Swagger UI in development:

```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ContosoShop Orders API V1");
    });
}
```

Access Swagger UI at: `https://localhost:5001/swagger`

## CORS Configuration

For development when Client and Server run on different ports:

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:5002")  // Blazor client URL
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowBlazorClient");
```

For production (same origin or Azure Static Web Apps):
- Configure CORS to allow specific frontend domain
- Never use `AllowAnyOrigin()` in production

## Alignment with Specifications

| Requirement | Contract Support |
|-------------|-----------------|
| FR-001: Display order list | GET /api/orders returns OrderSummary[] |
| FR-002: View order details | GET /api/orders/{id} returns OrderDetails |
| FR-006: Return action for delivered | POST /api/orders/{id}/return |
| FR-008: Update status to Returned | POST /api/orders/{id}/return changes status |
| FR-016: Appropriate HTTP codes | 200, 204, 400, 404, 500 defined |
| FR-017: Validate return eligibility | 400 error for non-delivered orders |
| Principle V: API-Driven | REST conventions, shared models, clear contracts |

## Next Steps

- [ ] Implement OrdersController following OpenAPI spec
- [ ] Add Swashbuckle.AspNetCore NuGet package for Swagger
- [ ] Create contract tests using WebApplicationFactory
- [ ] Validate JSON serialization of all models
- [ ] Test error responses match ProblemDetails format
