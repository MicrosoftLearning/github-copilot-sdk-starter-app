# ContosoShop E-commerce Support Portal (Local Edition)

**Project Name:** ContosoShop E-commerce Support Portal (Local Edition)

## Overview

ContosoShop E-commerce Support Portal is a sample web application that simulates an online store's customer support interface. It allows a user to view their orders, check order status, and initiate returns/refunds through a self-service portal. The project is designed as a production-ready application that runs locally (using a lightweight SQLite database and local email logging) while being architecturally ready to migrate to cloud services (such as Azure SQL Database, Azure App Service, and Azure email services).

## Key Features

- **Secure Authentication & Authorization:** The application MUST implement proper user authentication using ASP.NET Core Identity or JWT tokens. All users MUST log in before accessing order data. Authorization checks MUST ensure users can only view and manage their own orders (no access to other users' data).

- **Order History & Details:** Authenticated users can view a list of their own past orders and see detailed information for each order (order items, status, dates). All order access MUST be protected with ownership validation.

- **Order Status Tracking:** The application shows the current status of each order (e.g., *Processing*, *Shipped*, *Delivered*, *Returned*). This data is stored in a local SQLite database for easy setup and can be migrated to Azure SQL for production with encryption enabled.

- **Secure Returns/Refunds:** For delivered orders, authenticated users can initiate item-level returns for their own orders only. Users can return individual items or partial quantities with required justifications. The API MUST validate order ownership and eligibility before processing returns. CSRF protection MUST be implemented for this state-changing operation. The logic is contained in the backend API and designed to be expanded or connected to real payment systems later. Returns automatically update inventory levels.

- **Inventory Management System:** The application includes a comprehensive inventory tracking system with 2,500 serialized items across 25 product types (100 units per product). The system automatically manages inventory reservations when orders are placed and restores inventory when returns are processed. Features include:
  - Product catalog with pricing, weights, and dimensions
  - Serial number tracking for each inventory item (format: ITM-XXX-YYYY)
  - Status tracking (In Stock, Reserved, Returned) with return history flags
  - FIFO (First In, First Out) inventory allocation logic
  - Real-time inventory counts and availability
  - View Inventory page with detailed product and stock level information
  - Automatic coordination between orders, returns, and inventory levels

- **Contact Support (to be enhanced):** The application includes a "Contact Support" page. Initially, this page provides guidance on how to reach customer service (and may allow submitting a support request form).

- **Blazor WebAssembly Frontend:** A rich client-side UI built with Blazor WebAssembly provides a responsive single-page application experience. The UI is implemented with production best practices (e.g., responsive layout, error handling, loading indicators, secure authentication flows) and communicates with the backend via authenticated HTTP API calls.

- **Secure ASP.NET Core Web API Backend:** A robust backend powered by ASP.NET Core Web API (.NET 8) handles all business logic and data access with comprehensive security controls. It exposes RESTful endpoints (protected with `[Authorize]` attributes) for retrieving orders, updating order status, and other operations. The API MUST implement rate limiting, CSRF protection, secure CORS policies, and security headers. This separation ensures the frontend and backend are decoupled and can scale independently (or even be replaced by other client apps).

- **Local Development Friendly:** The app uses EF Core with a local SQLite database file for easy setup – no external dependencies needed to run locally. The database is seeded with sample data (two demo user accounts: Mateo Gomez / mateo@contoso.com and Megan Bowen / megan@contoso.com, password: Password123! for both, plus ~10 orders per user) so the app works out-of-the-box. The database also includes a complete product catalog (25 products) and inventory system (2,500 serialized items with 100 units per product) that automatically coordinates with orders and returns. For emailing (e.g., sending a confirmation when a refund is processed), the base app simply logs the email content to the console, avoiding external email service requirements during development.

- **Cloud-Ready Architecture:** Although running locally, the app's architecture aligns with cloud deployment practices. Configuration is managed via appsettings.json (with override support for environment-specific settings), making it easy to switch connection strings or service URLs for cloud environments. The application is divided into projects (Client and Server, plus shared models) similar to the Blazor WASM Hosted model, facilitating deployment to Azure App Service (for the API) and Azure Static Web Apps or Azure Storage (for the Blazor client). The EF Core data access layer can point to Azure SQL by changing a connection string, and the email service can be swapped with an actual email provider (like SendGrid) without changing the core logic.

## Running the App Locally

1. **Prerequisites:** .NET 8 SDK or later is required (the project targets .NET 8). You'll also need a recent version of Node.js if using any build steps for front-end (Blazor WASM doesn't require Node for standard use). Visual Studio 2022 or VS Code with the C# extension is recommended for editing and running the project.

2. **Clone the Repository:** Retrieve the project source code (the exact steps depend on how the lab provides the code – typically by downloading or cloning a GitHub repository).

3. **Database Setup:** The project includes a SQLite database file (App_Data/ContosoShop.db for example) with seed data including two demo user accounts for testing authentication (Mateo Gomez / mateo@contoso.com and Megan Bowen / megan@contoso.com, both with password Password123!). Each user has approximately 10 orders with various statuses. EF Core Migrations have been run and the database is up-to-date. There's no additional setup needed; the database will be copied on build if not present.

4. **Configure (optional):** By default, the app uses the included SQLite DB and defaults to development settings. No modification is necessary for the lab scenario. **Security Note:** User secrets or environment variables MUST be used for any sensitive configuration (never commit secrets to source control). If you want to test using a different database (e.g., SQL Server), update the connection string in appsettings.json and ensure the database is reachable.

5. **Build and Run:** Open the solution in Visual Studio and press **F5** (or use dotnet run on the API project and a static file server for the Blazor client, if running manually). The backend API will start (e.g., on https://localhost:5001) and the Blazor client will be served (e.g., on https://localhost:5002 or via the same server depending on configuration). By default, the solution is set up to run both projects together.

6. **Using the App:** In your browser, navigate to the provided URL (usually https://localhost:5002 for the Blazor app). You will be prompted to log in (use the seeded demo credentials). After authentication, you can click "Orders" to view sample orders for the logged-in user. Clicking an order will show its details. If an order is delivered and eligible for return, a "Return Order" button will be visible (protected with CSRF tokens). The Contact Support page is also accessible (it currently shows contact info or a placeholder).

7. **Observe Logs:** The backend API will output console logs for key events. For example, if you initiate a return, the backend might log a message that a refund email was "sent" (simulated). **Security Note:** Logs will contain sanitized information only - no PII (email addresses, amounts) will appear in console logs per security requirements. These logs appear in the output window of VS or the console where dotnet run was executed.

## Cloud Deployment Path

While this lab runs everything locally for simplicity, the app is prepared for cloud deployment with enterprise-grade security. To deploy to Azure, you MUST implement the following:

**Security Requirements for Production:**

- **Azure Key Vault Integration:** ALL secrets (database connection strings, API keys, JWT signing keys) MUST be stored in Azure Key Vault, NOT in configuration files
- **Managed Identity:** Use Azure Managed Identity for accessing Key Vault and other Azure resources (no hardcoded credentials)
- **Azure SQL Database:** For production, MUST migrate from SQLite to Azure SQL Database with:
  - Transparent Data Encryption (TDE) enabled
  - Firewall rules restricting access to Azure services only
  - Encrypted connections (SSL/TLS enforced)
  - Automated backups configured
- **Azure App Service Security:** If hosting on App Service:
  - HTTPS only (redirect HTTP to HTTPS)
  - Minimum TLS version 1.2
  - Authentication/authorization configured (Azure AD B2C or App Service Authentication)
  - Security headers middleware enabled
- **Network Security:** Configure Virtual Network integration and private endpoints for database access
- **Monitoring & Alerts:** Configure Azure Monitor and Application Insights with alerts for:
  - Failed authentication attempts
  - Rate limit violations
  - Unauthorized access attempts
  - API errors and exceptions

**Deployment Steps:**

- Host the ASP.NET Core Web API on **Azure App Service** (or Azure Container Apps). Publish the Server project, and switch the EF Core provider to Azure SQL by updating the connection string (stored in Key Vault) to point to an Azure SQL Database. The same EF Core migrations apply – you can run them on Azure or generate SQL scripts to set up the schema in the cloud.

- Host the Blazor WASM client on **Azure Static Web Apps** or as part of the App Service. In a production scenario, you might combine the deployment so that the API and Blazor UI are served from the same domain for simplicity. The project structure supports this (the Blazor app can be published into the API's wwwroot if desired for a single deployment unit, or deployed separately as a static site).

- Integrate **Azure Services** as needed: e.g., swap out the email logger with an **Azure SendGrid** integration (API key in Key Vault) to send real emails, plug in **Application Insights** for monitoring and diagnostics with security event tracking, and consider using **Azure OpenAI Service** to host the AI model behind the Copilot SDK (if you want full control of the AI in production rather than relying on the GitHub Copilot service).

This README provides a high-level orientation. For more details on what the app does and how it's built, see the **AppFeatures** and **TechStack** documentation below. Happy coding!
