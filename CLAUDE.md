# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

**Grevity** is an ASP.NET Core 8.0 MVC billing and inventory management application. Key concerns: user auth, multi-company session switching, customers/suppliers with running balances, products/sub-products with bill-of-materials composition, sales/purchase documents with stage workflows, and audit logging. There is no separate test project.

## Commands

```bash
dotnet restore
dotnet build Grevity.sln
dotnet run --launch-profile https          # ports 7029 (HTTPS) + 5247 (HTTP)
dotnet run --launch-profile swapnil        # developer-specific environment
dotnet ef database update
```

## Architecture

**Request flow:** `Controller → Service → Repository<T> / AppDbContext`

- `Program.cs` — DI wiring, middleware order (HTTPS → static files → routing → session → auth → authorization), default route `{controller=Home}/{action=Index}/{id?}`
- `Data/AppDbContext.cs` — EF Core with SQL Server; wraps `SaveChangesAsync` to write `AuditLog` rows after every save; applies global query filters for multi-tenant isolation on 8 entity types
- `Services/` — all domain logic; some services bypass the generic repository and use `AppDbContext` directly for complex queries, transactions, or `IgnoreQueryFilters()`
- `Repositories/` — thin generic CRUD wrapper; **every add/update/delete immediately calls SaveChanges** — no unit-of-work, no transaction coordination across calls
- `Models/Entities/` — `BaseEntity` (Id, timestamps), `ICompanyEntity` (CompanyId); new `ICompanyEntity` instances get `CompanyId` auto-assigned during save when missing

## Multi-Company / Session Tenancy

- Active company stored in session key `ActiveCompanyId`, managed by `ICompanyContext`
- `BusinessSettingService.GetSettingsAsync()` resolves: explicit ID supplied → active company → first company record
- Query filters: `!e.CompanyId.HasValue || e.CompanyId == currentCompanyId` — null-CompanyId entities are visible to all companies; preserve this unless the task explicitly changes multi-tenant visibility
- `BusinessSettingService.DeleteSettingsAsync()` cascades deletes across invoices, items, payments, products, customers, suppliers, audit logs, and user-company mappings — treat as high-risk

## Auth

- Cookie-based; 8-hour sliding expiry; login path `/Account/Login`
- Claims: `ClaimTypes.Name` (username), `ClaimTypes.Role`, custom `UserId`
- **Passwords are stored in plaintext** in `PasswordHash` — known issue, do not fix incidentally
- OTP stored plaintext on the `User` entity with 15-minute expiry; cleared on successful password reset
- `BusinessSetting.EmailPassword` is protected via ASP.NET Core Data Protection API; `EmailService` is a dev stub (hardcoded Gmail SMTP, empty credentials, falls back to `Debug.WriteLine`)

## Products and Sub-Products

- `Product` can be composed from `SubProduct` items via `ProductSubProductMapping` (bill-of-materials)
- Stock validation for a product sale checks required sub-product quantities across its mappings
- `SubProductService.DeleteSubProductAsync()` is a **soft delete** — refuses if sub-product is in any mapping, otherwise sets `IsActive = false`
- `ProductService.UpdateProductCompositionAsync()` removes all existing mappings then re-adds new ones one-at-a-time through the repository (multiple DB commits)

## Invoice / Document Workflow

- `Invoice` is shared for sales and purchases, distinguished by `InvoiceType` (`"Sale"` / `"Purchase"`)
- `DocumentStage`: `Draft → Quotation → Order → Invoice → Cancelled`
- Stock deductions and customer/supplier balance effects are applied **only** when stage transitions to `Invoice`
- `UpdateStageAsync()` does **not** reverse stock/balance effects when moving away from `Invoice` — known gap
- `DeleteInvoiceAsync()` does **not** roll back stock, balances, or payment transactions — known gap
- Invoice numbers use `{sequence}-{financialYearSuffix}` format, e.g. `1-26`
- `Invoice.PaymentStatus` is computed from `PaidAmount` vs `GrandTotal`; `Status` is stored separately but kept in sync

## Audit Logging

- `AppDbContext.SaveChangesAsync()` captures all entity changes, writes `AuditLog` rows after the main save
- Skips unchanged entities and `AuditLog` itself
- Records entity type, primary key, old/new values (JSON), user, company, and timestamp
- Be careful when changing transaction scope, save ordering, batch updates, or using direct SQL — audit capture can be bypassed or doubled

## Frontend

- Server-rendered Razor views + Bootstrap, jQuery, jQuery Validation, HTMX, NProgress, Google Material Symbols
- `_Layout.cshtml` sets `hx-boost="true"` on `<body>` — HTMX intercepts normal link clicks for AJAX navigation; this affects all pages, not just those with explicit `hx-*` attributes
- Invoice and purchase create/edit screens contain substantial **inline JavaScript** for dynamic line-item rows, totals, and payment state — not in `site.js`
- No bundler; libraries are linked via individual `<script>` / `<link>` tags

## Smoke Tests (no automated tests exist)

After any change, run `dotnet build Grevity.sln` first. Then test the relevant area:

| Changed area | Smoke test |
|---|---|
| Auth / settings | register, login, company switch, settings save, company list |
| Products / sub-products | create/edit product, create/edit sub-product, composition mapping, stock availability |
| Invoices / purchases | create, edit, details, stage conversion, balance and stock impact |
| Reports | sales, purchases, consolidated, outstanding pages, Excel export |

## Known Caveats

- Build emits nullable-reference warnings and migration class-name warnings — pre-existing noise
- `bin/` and `obj/` are tracked in git; `git status` is noisy after builds
- `DinkToPdf` is in `.csproj` but has no current source usage
- Payment transaction service methods exist but there is no payment UI or controller
