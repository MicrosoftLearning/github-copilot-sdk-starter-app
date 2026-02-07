<!--
========================================
SYNC IMPACT REPORT - Constitution Update
========================================
Version Change: 1.0.0 → 2.0.0
Date: 2026-02-05

CHANGES MADE:
- MAJOR UPDATE: Enhanced security principles from stakeholder document review
- Added comprehensive authentication/authorization requirements (Principle I)
- Added CSRF protection requirements (Principle I)
- Added rate limiting requirements (Principle I)
- Added input validation requirements (Principle I)
- Added secure logging requirements with PII sanitization (Principle I)
- Enhanced CORS security configuration (Principle IV)
- Added security headers middleware requirements (Principle I)
- Added database security requirements (Principle IV)
- Updated all principles with actionable security controls

PRINCIPLES MODIFIED:
1. Security-First Design (ENHANCED) - Added mandatory authentication, authorization, CSRF protection, rate limiting, PII sanitization
2. Testable Architecture (UNCHANGED)
3. Code Quality Standards (UNCHANGED)
4. Cloud-Ready Design (ENHANCED) - Added secure CORS, database encryption, Azure Key Vault requirements
5. API-Driven Development (ENHANCED) - Added authorization enforcement, security headers

VERSION BUMP RATIONALE:
- MAJOR (2.0.0): Backward-incompatible changes to security architecture
- Previously optional/implied security is now mandatory and specific
- Authentication requirement breaks existing demo-only implementation
- CSRF protection, rate limiting, and authorization checks are breaking changes

TEMPLATE CONSISTENCY STATUS:
✅ plan-template.md - Constitution Check section compatible
✅ spec-template.md - Requirements approach aligns with enhanced security principles
✅ tasks-template.md - Task organization supports security feature implementation
⚠ Future specs MUST include security validation tasks for all features

FOLLOW-UP TODO:
- All existing implementations MUST be updated to comply with enhanced security requirements
- Authentication/authorization system MUST be implemented before next production deployment
- Rate limiting middleware MUST be configured
- CSRF protection MUST be added to all state-changing endpoints
========================================
-->

# ContosoShop E-commerce Support Portal Constitution

## Core Principles

### I. Security-First Design

All features MUST be designed with security as the primary concern. This principle is NON-NEGOTIABLE for production deployment:

**Authentication & Authorization (MANDATORY):**
- The application MUST implement ASP.NET Core Identity or JWT-based authentication before production deployment
- All API endpoints that access user data MUST use `[Authorize]` attribute
- Users MUST authenticate before accessing any order data or performing actions
- Authorization checks MUST verify `order.UserId == currentAuthenticatedUserId` for all order operations
- Unauthorized access attempts MUST return HTTP 403 Forbidden
- User identity MUST be obtained from authenticated claims via `HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)`
- Failed authentication attempts MUST be logged for security monitoring

**CSRF Protection (MANDATORY):**
- All state-changing operations (POST, PUT, DELETE) MUST validate anti-forgery tokens
- Cookie-based authentication MUST use HttpOnly, Secure, and SameSite attributes
- CSRF token validation MUST be enforced via `[ValidateAntiForgeryToken]` or equivalent middleware

**Input Validation (MANDATORY):**
- All user inputs MUST be validated on both client and server sides
- Server-side validation is CRITICAL - never trust client validation alone
- Model validation attributes (`[Required]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`) MUST be enforced
- Invalid inputs MUST return HTTP 400 Bad Request without exposing system internals
- SQL injection MUST be prevented through parameterized queries (EF Core LINQ only, no raw SQL)
- XSS vulnerabilities MUST be prevented through proper encoding (Blazor handles automatically)

**Rate Limiting (MANDATORY):**
- API MUST implement rate limiting middleware (e.g., `AspNetCoreRateLimit`)
- Authentication endpoints: maximum 5 failed attempts per 15 minutes per IP
- Order listing: maximum 60 requests per minute per user
- Order details: maximum 120 requests per minute per user
- Return operations: maximum 10 requests per hour per user
- Rate limit violations MUST return HTTP 429 Too Many Requests

**Secure Logging (MANDATORY):**
- Logs MUST NOT contain Personally Identifiable Information (PII)
- Email addresses MUST be masked or hashed in logs
- Financial amounts MUST be logged generically ("Refund processed" not "$59.99")
- Security events (auth failures, unauthorized access) MUST be logged with timestamps and IP addresses
- Audit trails MUST be maintained for all state-changing operations

**Security Headers (MANDATORY):**
- MUST implement security headers middleware:
  - `X-Content-Type-Options: nosniff` (prevent MIME sniffing)
  - `X-Frame-Options: DENY` (prevent clickjacking)
  - `X-XSS-Protection: 1; mode=block`
  - `Content-Security-Policy` (restrict resource loading)
  - `Strict-Transport-Security` (HSTS for HTTPS enforcement)

**Transport Security:**
- HTTPS MUST be enforced for all API communication
- HTTP requests MUST redirect to HTTPS
- HSTS MUST be enabled in production environments

**Secrets Management:**
- Connection strings, API keys, and credentials MUST NEVER be committed to source control
- Development: Use .NET User Secrets or environment variables
- Production: Use Azure Key Vault with Managed Identity
- Secrets MUST be externalized via configuration (appsettings.json overrides)

**Rationale**: As an e-commerce support portal handling customer orders and authentication, security failures could result in data breaches, unauthorized access, PII exposure, financial loss, and loss of customer trust. Security MUST be enforced at every layer, not as an afterthought.

### II. Testable Architecture

Code MUST be structured to enable comprehensive testing:

- Dependency Injection MUST be used for all service dependencies
- Interfaces MUST be defined for external dependencies (database, email, external APIs)
- Business logic MUST be separated from presentation and data access layers
- Each service class MUST have a single, well-defined responsibility
- Mock implementations MUST be available for development and testing (e.g., EmailServiceDev)
- Integration tests MUST verify API contracts and end-to-end workflows

**Rationale**: Testability ensures maintainability, enables confident refactoring, and reduces regression risk as the codebase evolves.

### III. Code Quality Standards

All code MUST meet established quality standards:

- Code MUST follow .NET naming conventions and C# coding standards
- All public APIs and classes MUST have XML documentation comments
- Code reviews MUST be conducted for all changes
- Static analysis tools MUST be configured and warnings addressed
- Code complexity MUST be justified if cyclomatic complexity exceeds reasonable thresholds
- Consistent formatting MUST be maintained (EditorConfig or similar)
- No commented-out code or TODO comments in production branches without tracking issues

**Rationale**: Consistent, high-quality code reduces cognitive load, eases onboarding, and prevents technical debt accumulation.

### IV. Cloud-Ready Design

Applications MUST be architected for seamless cloud migration with enterprise-grade security:

**Configuration Externalization:**
- Configuration MUST be externalized (appsettings.json, environment variables, Azure App Configuration)
- Secrets MUST be stored in Azure Key Vault (production) or User Secrets (development)
- Application MUST use Managed Identity for accessing Azure resources (no hardcoded credentials)
- Environment-specific configurations MUST NOT require code changes

**Database Abstraction & Security:**
- Database access MUST use abstraction layers (EF Core) to enable provider switching (SQLite → Azure SQL)
- Local development: SQLite database file MUST NOT be web-accessible (outside wwwroot)
- Production: MUST migrate to Azure SQL Database with:
  - Transparent Data Encryption (TDE) enabled
  - Firewall rules restricting access to Azure services only
  - Encrypted connections (SSL/TLS enforced)
  - Automated backups configured
- Connection strings MUST be stored securely (Azure Key Vault in production)

**Service Abstraction:**
- Service implementations MUST be swappable through DI (local file logging → Azure Application Insights)
- Email service MUST support multiple backends (console logging → SendGrid/Azure Communication Services)
- Authentication MUST support multiple providers (local → Azure AD B2C)

**CORS Security (CRITICAL):**
- CORS MUST be restrictively configured - NEVER use `AllowAnyOrigin()`, `AllowAnyMethod()`, or `AllowAnyHeader()`
- MUST explicitly whitelist only required origins (e.g., `https://localhost:5002`, production domain)
- MUST explicitly whitelist only required HTTP methods (GET, POST - not DELETE/PUT unless needed)
- MUST explicitly whitelist only required headers (Content-Type, Authorization)
- Example secure configuration:
  ```csharp
  policy.WithOrigins("https://localhost:5002", "https://yourdomain.com")
        .WithMethods("GET", "POST")
        .WithHeaders("Content-Type", "Authorization")
        .AllowCredentials();
  ```

**Static Resources:**
- Static resources MUST be deployable to CDN/static hosting (Azure Storage, Azure Static Web Apps)
- Build output MUST be optimized for CDN delivery (compression, caching headers)

**Logging & Monitoring:**
- Logging MUST target console/structured output compatible with Azure Monitor
- PII MUST be sanitized from all logs (see Security-First Design principle)
- Application Insights integration MUST be supported for production environments
- Security alerts MUST be configured for:
  - Failed authentication attempts
  - Rate limit violations
  - Unauthorized access attempts
  - API errors and exceptions

**Network Security:**
- Production deployments MUST support Virtual Network integration
- Database connections MUST use private endpoints when available
- Public access MUST be restricted via Azure Front Door or Application Gateway

**Rationale**: The project is designed for local development but production deployment on Azure. Cloud-ready design with security hardening from day one eliminates costly refactoring and security retrofitting during migration. Production deployments MUST enforce enterprise-grade security controls.

### V. API-Driven Development

All client-server communication MUST follow REST API principles with mandatory security enforcement:

**API Design & Contracts:**
- Backend MUST expose well-defined REST endpoints with clear contracts
- API responses MUST use consistent HTTP status codes:
  - 200 OK (success)
  - 400 Bad Request (validation errors)
  - 401 Unauthorized (authentication required)
  - 403 Forbidden (insufficient permissions)
  - 404 Not Found (resource doesn't exist)
  - 429 Too Many Requests (rate limit exceeded)
  - 500 Internal Server Error (system errors)
- API models MUST be shared between client and server (ContosoShop.Shared project)
- Error responses MUST include meaningful messages for client handling (without exposing system internals)

**Security Enforcement (MANDATORY):**
- ALL endpoints that access user data MUST use `[Authorize]` attribute
- ALL endpoints MUST validate user ownership of requested resources
- ALL state-changing endpoints (POST, PUT, DELETE) MUST validate anti-forgery tokens
- API versioning strategy MUST be considered for breaking changes
- Security headers MUST be included in all API responses

**API Documentation:**
- API documentation MUST be maintained (Swagger/OpenAPI when appropriate)
- Authentication requirements MUST be documented for each endpoint
- Authorization rules MUST be documented for each endpoint
- Rate limits MUST be documented for each endpoint
- Error responses MUST be documented with example payloads

**API Testing:**
- API endpoints MUST be independently testable through contract tests
- Integration tests MUST verify end-to-end workflows with authentication
- Security tests MUST verify authorization enforcement
- Rate limiting MUST be verified through load tests

**Rationale**: Clear API contracts with enforced security enable independent frontend/backend development, simplify testing, support potential future clients (mobile apps, third-party integrations), and ensure consistent security enforcement across all endpoints. APIs without authorization checks create critical security vulnerabilities.

## Security Requirements

All development MUST adhere to these comprehensive security standards:

**Authentication & Identity (MANDATORY):**
- Users MUST authenticate before accessing any order data or performing actions
- Authentication system MUST support ASP.NET Core Identity or JWT tokens
- Multi-factor authentication SHOULD be supported for production deployments
- Session management MUST implement reasonable timeout periods
- Password policies MUST enforce minimum complexity requirements
- Authentication state MUST be managed securely across client and server

**Authorization & Access Control (MANDATORY):**
- API endpoints MUST verify user ownership of resources (`order.UserId == authenticatedUserId`)
- Users MUST only access their own orders and data (strict data isolation)
- Role-based access control SHOULD be implemented for administrative functions
- Authorization failures MUST return HTTP 403 Forbidden with sanitized error messages

**Data Protection (MANDATORY):**
- Customer orders, user data, and PII MUST be handled according to GDPR and data protection principles
- Sensitive data MUST be encrypted at rest (database TDE) and in transit (TLS/HTTPS)
- PII MUST NOT appear in logs, error messages, or client-side code
- Data retention policies MUST be documented and enforced
- Personal data MUST be deletable upon request (right to erasure)

**Secrets Management (MANDATORY):**
- Connection strings, API keys, JWT signing keys, and credentials MUST NEVER be committed to source control
- Development: Use .NET User Secrets or secure environment variables
- Production: Use Azure Key Vault with Managed Identity (no connection strings in code)
- Secrets rotation procedures MUST be documented

**Attack Prevention (MANDATORY):**
- SQL Injection: Use EF Core LINQ only, NEVER raw SQL with string concatenation
- XSS: Rely on Blazor's automatic HTML encoding, never use unescaped content
- CSRF: Validate anti-forgery tokens for all state-changing operations
- Clickjacking: Implement X-Frame-Options: DENY header
- MIME Sniffing: Implement X-Content-Type-Options: nosniff header
- Rate Limiting: Enforce request limits to prevent DoS and brute force attacks

**Audit Logging (MANDATORY):**
- Security-relevant events MUST be logged:
  - Authentication attempts (success and failure)
  - Authorization failures
  - Data access (order views, modifications)
  - Administrative actions
  - Rate limit violations
- Logs MUST include: timestamp, user ID (if authenticated), action, IP address, user agent
- Logs MUST NOT include: passwords, tokens, PII, financial details
- Audit logs MUST be tamper-evident and retained per compliance requirements

**Network Security (MANDATORY):**
- HTTPS MUST be enforced for all communication (UseHttpsRedirection)
- HSTS MUST be enabled in production (UseHsts)
- TLS 1.2 or higher MUST be required
- CORS MUST be explicitly configured (never wildcard)
- Security headers MUST be implemented (CSP, X-Frame-Options, etc.)

**Production Deployment Security:**
- Azure Key Vault MUST be used for all secrets
- Managed Identity MUST be used for Azure resource access
- Azure SQL MUST use TDE, firewall rules, and private endpoints
- Network isolation MUST be configured (Virtual Networks)
- Azure Monitor alerts MUST be configured for security events
- Regular security scans MUST be performed (dependency vulnerabilities, code analysis)

## Development Workflow

All development activities MUST follow this workflow:

### Feature Development

1. Feature specifications MUST be created in `.specify/specs/[###-feature-name]/spec.md` before implementation
2. Implementation plans MUST be documented in `.specify/specs/[###-feature-name]/plan.md`
3. Tasks MUST be broken down and tracked in `.specify/specs/[###-feature-name]/tasks.md`
4. Feature branches MUST follow naming convention: `[###-feature-name]`

### Code Reviews

1. All changes MUST be submitted via pull requests
2. PRs MUST include description of changes, test results, and constitution compliance verification
3. At least one approval MUST be obtained before merging
4. Automated checks (build, tests, linting) MUST pass before merge consideration

### Testing Requirements

1. Business logic changes MUST include unit tests
2. New API endpoints MUST include contract/integration tests
3. **Security tests MUST verify:**
   - Authentication enforcement on protected endpoints
   - Authorization checks (users can only access own data)
   - CSRF token validation on state-changing operations
   - Input validation and sanitization
   - Rate limiting behavior
4. Tests MUST be written before or alongside implementation (not deferred)
5. Test coverage SHOULD be monitored (aim for >70% for critical business logic, >90% for security-critical code)

### Documentation

1. README files MUST be updated when setup/deployment procedures change
2. API changes MUST be documented in code comments and/or OpenAPI specs
3. Architecture decisions MUST be captured in ADR (Architecture Decision Records) format when significant

## Governance

This constitution supersedes all other development practices and guidelines. All team members and AI development agents MUST adhere to these principles.

### Amendment Process

1. Constitution amendments MUST be proposed with clear justification
2. Amendments MUST include impact analysis on existing codebase and templates
3. Version MUST be incremented according to semantic versioning:
   - MAJOR: Backward-incompatible changes to core principles
   - MINOR: New principles or substantial expansions
   - PATCH: Clarifications, wording improvements, non-semantic refinements
4. Amendments MUST be documented in the Sync Impact Report
5. All dependent templates and documentation MUST be updated to reflect amendments

### Compliance Verification

1. All pull requests MUST verify compliance with constitution principles
2. Constitution violations MUST be justified in `.specify/specs/[###-feature-name]/plan.md` Complexity Tracking section
3. Automated tooling SHOULD be used where possible to enforce standards (linters, analyzers, CI checks)
4. Regular audits SHOULD be conducted to identify drift from constitutional principles

### Constitution Authority

1. In conflicts between constitution and other documentation, constitution takes precedence
2. Feature specifications MUST align with constitutional principles
3. Technical decisions that contradict constitution MUST be explicitly documented and approved

**Version**: 2.0.0 | **Ratified**: 2026-02-04 | **Last Amended**: 2026-02-05
