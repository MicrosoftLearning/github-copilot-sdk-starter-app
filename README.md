# ContosoShop E-commerce Support Portal

A customer self-service support portal for e-commerce built with Blazor WebAssembly and ASP.NET Core Web API.

## Features

- **View Order History** - Browse all past orders with status tracking and color-coded badges
- **View Order Details** - See detailed information including items, timeline, pricing, and delivery status
- **Initiate Returns** - Process returns for delivered orders with automatic refund processing
- **Contact Support** - Access support resources, contact information, and AI chat (coming soon)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Windows, macOS, or Linux operating system
- A modern web browser (Chrome, Firefox, Edge, or Safari)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd ContosoShopSupportPortal
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build
```

The build should complete successfully. If you encounter errors, ensure .NET 8 SDK is properly installed.

### 4. Run the Application

```bash
dotnet run --project ContosoShop.Server
```

The application will start and be available at:
- **Application URL**: `http://localhost:5266`

The server hosts both the API and the Blazor WebAssembly client.

### 5. Access the Application

Open your browser and navigate to **http://localhost:5266**

The database will be automatically created and seeded with sample data on first run.

## Demo Data

The application is seeded with:
- **Demo User**: John Doe (john.doe@example.com)
- **Sample Orders**: 4 orders with various statuses (Processing, Shipped, Delivered, Returned)

## Using the Application

1. **Home Page** - View welcome message and quick action cards
2. **View Orders** - Click "Orders" in the navigation to see all orders
3. **View Order Details** - Click "View Details" on any order to see full information
4. **Process Return** - On a delivered order, click "Return Order" to initiate a return
5. **Contact Support** - Click "Contact Support" for help resources

## Project Structure

```
ContosoShopSupportPortal/
├── ContosoShop.Client/          # Blazor WebAssembly frontend
│   ├── Pages/                   # Razor pages (Home, Orders, OrderDetails, Support)
│   ├── Shared/                  # Shared components (OrderStatusBadge)
│   ├── Layout/                  # Layout components (MainLayout, NavMenu)
│   ├── Services/                # Client-side services (OrderService)
│   └── wwwroot/                 # Static assets (CSS, icons)
├── ContosoShop.Server/          # ASP.NET Core Web API backend
│   ├── Controllers/             # API controllers (OrdersController)
│   ├── Services/                # Business logic services (EmailService, OrderService)
│   ├── Data/                    # Database context and initialization
│   └── appsettings.json         # Configuration
└── ContosoShop.Shared/          # Shared models and contracts
    └── Models/                  # Entity models (User, Order, OrderItem, OrderStatus)
## Database

The application uses SQLite for local development with automatic seeding of sample data on first run.

**Demo User**: John Doe (john.doe@example.com)  
**Sample Orders**: 4 orders with various statuses (Processing, Shipped, Delivered, Returned)

## Development

### Running Tests

```bash
dotnet test
```

### Entity Framework Migrations

```bash
# Create migration
dotnet ef migrations add <MigrationName> --project ContosoShop.Server

# Apply migrations
dotnet ef database update --project ContosoShop.Server
```

## Configuration

Configuration is managed through `appsettings.json` files:

- `ContosoShop.Server/appsettings.json` - Base configuration
- `ContosoShop.Server/appsettings.Development.json` - Development overrides

### Key Settings

- **ConnectionString**: SQLite database path (default: `Data Source=App_Data/ContosoShop.db`)
- **Logging**: Log levels for different components
- **CORS**: Allowed origins for Blazor client

## Architecture

- **Frontend**: Blazor WebAssembly with Bootstrap 5 for responsive UI
- **Backend**: ASP.NET Core Web API with REST endpoints
- **Data Access**: Entity Framework Core with SQLite (development) or Azure SQL (production)
- **Dependency Injection**: Built-in ASP.NET Core DI for testability
- **API Contracts**: RESTful design with standard HTTP status codes

## Cloud Deployment

The application is designed for cloud deployment to Azure:

- **API**: Azure App Service
- **Client**: Azure Static Web Apps or CDN
- **Database**: Azure SQL Database
- **Configuration**: Azure App Configuration

To switch to Azure SQL, update the connection string in Azure App Service configuration and change `UseSqlite()` to `UseSqlServer()` in `Program.cs`.

## Success Criteria

- Order list loads in under 2 seconds
- Order details load in under 1 second
- Return operation completes in under 30 seconds
- Responsive UI from 320px (mobile) to 1920px (desktop)
- All API responses under 500ms

## Contributing

1. Create a feature branch from `main`
2. Make your changes following the `.editorconfig` conventions
3. Ensure all tests pass
4. Submit a pull request

## License

This is a sample/training application for demonstration purposes.

## Support

For questions or issues, see the Contact Support page in the application.
