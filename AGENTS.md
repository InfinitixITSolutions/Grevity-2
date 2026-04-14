# Grevity Agent Guide

## What This Repo Is
- Grevity is a single-project ASP.NET Core MVC billing and inventory app targeting `.NET 8`.
- The solution currently contains one app project: `Grevity.csproj`.
- Core concerns in the codebase are:
  - user auth and password reset
  - multi-company selection through session
  - customers and suppliers with running balances
  - products, sub-products, and product composition
  - sales and purchase documents that share one invoice service
  - audit logging and reporting
- There is no separate automated test project in this repo right now.

## Repo Map
- `Program.cs`
  - application startup, DI registration, auth, session, and routing
- `Data/AppDbContext.cs`
  - EF Core model registration, multi-company query filters, and audit-log persistence
- `Controllers/`
  - MVC endpoints and page orchestration
- `Services/Interfaces` and `Services/Implementations`
  - business logic layer
- `Repositories/Interfaces` and `Repositories/Implementations`
  - generic EF repository abstraction
- `Models/Entities`
  - persistence model and domain entities
- `Models/ViewModels`
  - page-level view models for dashboard, auth, and invoice screens
- `Views/`
  - Razor pages and shared partials
- `wwwroot/`
  - Bootstrap/jQuery assets, site CSS, and uploaded company logos
- `Migrations/`
  - EF migrations and model snapshot

## Canonical Commands
- `dotnet restore`
- `dotnet build Grevity.sln`
- `dotnet run --launch-profile https`
- `dotnet run --launch-profile swapnil`
- `dotnet ef database update`

## Runtime and Config Expectations
- Keep the current environment-specific files and launch profiles as-is unless the task explicitly asks to change them.
- Tracked config files currently include:
  - `appsettings.json`
  - `appsettings.Development.json`
  - `appsettings.Swapnil.json`
- Launch profiles live in `Properties/launchSettings.json`:
  - `http`
  - `https`
  - `swapnil`
  - `IIS Express`
- Preferred future local secret handling is ASP.NET Core user secrets, but hardening secrets is a separate task from normal feature work.
- This repo tracks generated `bin/` and `obj/` output. Treat changes there as build noise unless the user explicitly asks to work on tracked artifacts.
- `.gitignore` now ignores future `bin/` and `obj/` churn, but existing tracked build output will still appear in `git status` after builds.

## Startup and Request Pipeline
- `Program.cs` registers:
  - MVC with views
  - SQL Server `AppDbContext`
  - generic `IRepository<>`
  - auth, settings, product, customer, supplier, invoice, email, company-context, and sub-product services
  - `IHttpContextAccessor`
  - session support
- Middleware order is:
  - HTTPS redirection
  - static files
  - routing
  - session
  - authentication
  - authorization
- Default route is `{controller=Home}/{action=Index}/{id?}`.

## Architecture
- Request flow is `Controller -> Service -> Repository/AppDbContext`.
- The repository layer is generic and thin; domain-specific logic lives mostly in services and sometimes directly in `AppDbContext`.
- Repository methods save immediately on every add, update, and delete. There is no unit-of-work abstraction coordinating multiple repository calls.
- Some higher-risk workflows bypass the generic repository and use `AppDbContext` directly for includes, transactions, query-filter overrides, or batch deletes.

## Data Model
- `BaseEntity`
  - common `Id`, `CreatedAt`, and `UpdatedAt`
- `BusinessSetting`
  - acts as the company record and stores company profile, GST defaults, logo path, and protected email password
- `User`
  - username, email, role, password field, OTP code, OTP expiry
- `UserCompany`
  - maps users to accessible companies and marks the default company
- `Customer` and `Supplier`
  - company-scoped parties with opening and current balances
- `Product`
  - finished or purchased item with stock, price, GST, and optional composition from sub-products
- `SubProduct`
  - raw material style inventory item with stock, unit, cost, and soft-delete flag
- `ProductSubProductMapping`
  - bill-of-material style mapping between a product and required sub-products
- `Invoice`
  - shared sales and purchase header with `InvoiceType`, `Stage`, totals, paid amount, and computed payment status
- `InvoiceItem`
  - line item that can point to either a `Product` or a `SubProduct`
- `PaymentTransaction`
  - payment ledger rows tied to an invoice
- `AuditLog`
  - separate audit entity storing action, entity name/id, details JSON, user, company, and timestamp

## Multi-Company Rules
- Company context is session-driven via `Services/Implementations/CompanyContext.cs`.
- The active company session key is `ActiveCompanyId`.
- Login flow:
  - authenticates with `AccountController`
  - loads the user's default company from `UserCompany`
  - falls back to the first linked company if no default exists
- `BusinessSettingService.GetSettingsAsync()`:
  - uses an explicit ID if supplied
  - otherwise falls back to the active company
  - otherwise falls back to the first company record
- `AppDbContext` applies query filters to:
  - `Product`
  - `Customer`
  - `Supplier`
  - `Invoice`
  - `InvoiceItem`
  - `PaymentTransaction`
  - `SubProduct`
  - `ProductSubProductMapping`
- Important tenancy behavior:
  - entities with `CompanyId == null` remain visible because filters use `!e.CompanyId.HasValue || e.CompanyId == _companyContext.CurrentCompanyId`
  - preserve this unless the task explicitly changes multi-tenant visibility rules
- New entities implementing `ICompanyEntity` get `CompanyId` auto-assigned during save when it is missing.

## Persistence and Audit Rules
- `AppDbContext.SaveChangesAsync()` wraps all entity changes with audit capture.
- Audit behavior:
  - skips unchanged entities and `AuditLog` itself
  - records primary keys, old values, and new values
  - writes audit rows after the main save completes
- Be careful when changing:
  - transactions
  - batch updates
  - save ordering
  - direct SQL or bulk operations
- `BusinessSettingService.DeleteSettingsAsync()` is destructive:
  - it deletes invoices, items, payments, products, customers, suppliers, audit logs, and user-company mappings for the company
  - it uses `IgnoreQueryFilters()` for several entity sets

## Auth and Security Notes
- Auth is cookie-based with `UserId` and `Role` claims.
- Passwords are not actually hashed yet:
  - `AuthService.LoginAsync()` compares `PasswordHash` directly against the submitted password
  - `RegisterAsync()` stores the raw password in `PasswordHash`
  - `ResetPasswordAsync()` also writes the new password directly
- OTP flow:
  - stored on the `User` entity
  - generated with a 15-minute expiry
  - reset password clears OTP fields on success
- `SettingsController` encrypts `BusinessSetting.EmailPassword` with ASP.NET Core Data Protection before saving and attempts to unprotect it on load.
- `EmailService` is effectively a dev stub right now:
  - SMTP host is hardcoded to Gmail
  - sender email and password are empty strings
  - if required config is missing it falls back to `Debug.WriteLine`

## Controller and Page Map
- `AccountController`
  - login, register, forgot password, OTP verify, reset password, logout, switch company
- `HomeController`
  - dashboard counts and total sales summary
- `SettingsController`
  - current company settings, company list, logo upload, create company, delete company
- `CustomerController`
  - CRUD plus Excel import
- `SupplierController`
  - CRUD
- `ProductController`
  - CRUD, composition mapping, stock availability JSON endpoints
- `SubProductController`
  - CRUD, details JSON endpoint
- `InvoiceController`
  - sales document list/create/edit/details/delete, stage conversion, product-details JSON endpoint
- `PurchaseController`
  - purchase document list/create/edit/details/delete, stage conversion
- `ReportsController`
  - sales, purchases, outstandings, consolidated summary, Excel export
- `AuditLogsController`
  - latest 100 audit rows for the current company

## View and Frontend Structure
- Views are organized conventionally under `Views/<ControllerName>/`.
- Shared UI pieces include:
  - `Views/Shared/_Layout.cshtml`
  - `Views/Shared/_AuthLayout.cshtml`
  - `Views/Shared/_Sidebar.cshtml`
  - `Views/Shared/_CompanySwitcher.cshtml`
  - `Views/Shared/_Breadcrumbs.cshtml`
  - `Views/Shared/_Notifications.cshtml`
  - `Views/Shared/_LoginPartial.cshtml`
- Frontend stack is mostly server-rendered Razor plus:
  - Bootstrap
  - jQuery
  - jQuery validation
  - HTMX
  - NProgress
  - Google Material Symbols
- `_Layout.cshtml` sets `hx-boost="true"` on the `<body>`, so navigation behavior can be affected by HTMX even on normal-looking links.
- `wwwroot/js/site.js` is minimal; important page behavior is often implemented inline inside Razor views.
- The invoice and purchase create/edit screens contain substantial inline JavaScript for dynamic line-item rows, totals, and payment state.

## Business Workflow Notes
- Dashboard
  - `HomeController.Index()` shows customer count, product count, invoice count, and sum of `GrandTotal`

- Customers and suppliers
  - current balances are part of core business state
  - customer import uses `ClosedXML` to read an uploaded Excel file
  - settings-driven GST visibility is injected into customer and supplier detail/edit/create pages

- Products and sub-products
  - products can be `Sales` or `Purchase` items via `ItemType`
  - a product may optionally be composed from multiple sub-products
  - composition is stored in `ProductSubProductMapping`
  - `SubProductService.DeleteSubProductAsync()` is a soft delete:
    - it refuses deletion if the sub-product is used in any product mapping
    - otherwise it sets `IsActive = false`
  - stock validation for product sale checks required quantities across the mapped sub-products

- Sales and purchase documents
  - both flows share the same `Invoice` and `InvoiceService`
  - `InvoiceType` distinguishes `"Sale"` from `"Purchase"`
  - `DocumentStage` values are:
    - `Draft`
    - `Quotation`
    - `Order`
    - `Invoice`
    - `Cancelled`
  - stock and balance side effects are applied only when the stage is `Invoice`
  - invoice numbers are generated in `{sequence}-{financialYearSuffix}` format, for example `1-26`
  - line items can reference either:
    - `ProductId`
    - `SubProductId`
  - item snapshots store `ProductName` and `HSN` on the line

- Payments and balances
  - `Invoice.PaymentStatus` is computed from `PaidAmount` and `GrandTotal`
  - `Status` is stored separately but typically kept in sync with `PaymentStatus`
  - `InvoiceService` supports:
    - creating and editing invoices
    - adding payment transactions
    - manual payment override updates
    - stage conversion
  - There is no dedicated payment transaction controller or payment UI flow in the current source tree, even though service methods exist.

- Reports and exports
  - reports are generated directly from `AppDbContext`
  - `ReportsController.ExportSalesInternal()` uses `ClosedXML` to create an Excel workbook
  - `DinkToPdf` is referenced in `Grevity.csproj`, but no current source usage was found during this repo sweep

## High-Risk Behaviors to Watch
- `DeleteInvoiceAsync()` currently performs a direct repository delete with no explicit stock rollback, balance rollback, or payment-transaction cleanup logic in the service itself.
- `UpdateStageAsync()` applies stock and balance effects when moving into `DocumentStage.Invoice`, but it does not reverse those effects when moving away from that stage.
- Repository methods save immediately, so loops that call repository methods may trigger multiple database commits.
- `ProductService.UpdateProductCompositionAsync()` removes existing mappings, then adds new mappings one at a time through the repository.
- `BusinessSettingService.DeleteSettingsAsync()` performs broad cascading deletes for a company. Treat changes here as high-risk.

## Verification Guidance
- Baseline verification:
  - `dotnet build Grevity.sln`
- For auth or settings changes, smoke-test:
  - register
  - login
  - company switch
  - settings save
  - company list
- For product or sub-product changes, smoke-test:
  - create/edit product
  - create/edit sub-product
  - composition mapping
  - stock availability checks
- For invoice or purchase changes, smoke-test:
  - create
  - edit
  - details
  - stage conversion
  - customer or supplier balance impact
  - stock movement impact
- For report changes, smoke-test:
  - sales report
  - purchase report
  - consolidated report
  - outstanding pages
  - Excel export

## Known Repo Caveats
- No dedicated automated tests are present.
- `dotnet build` succeeds, but the repo currently emits many nullable-reference warnings plus warnings from lowercase migration class names.
- Build output is tracked in git, so `git status` becomes noisy after builds.
- Current environment-specific config is intentionally preserved; do not rename or delete it unless explicitly asked.
