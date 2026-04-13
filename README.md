# Grevity

**Multi-Company GST Billing & Inventory Management System**

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue)
![EF Core](https://img.shields.io/badge/Entity%20Framework-Core-purple)
![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5-blueviolet)
![License](https://img.shields.io/badge/License-MIT-green)

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Features](#features)
  - [Authentication](#1-authentication)
  - [Dashboard](#2-dashboard)
  - [Multi-Company Support](#3-multi-company-support)
  - [Customer Management](#4-customer-management)
  - [Supplier Management](#5-supplier-management)
  - [Sub-Product Management](#6-sub-product-management)
  - [Product Management](#7-product-management)
  - [Sales Invoice Management](#8-sales-invoice-management)
  - [Purchase Bill Management](#9-purchase-bill-management)
  - [Reports](#10-reports)
  - [Settings / Company Profile](#11-settings--company-profile)
  - [Audit Logs](#12-audit-logs)
- [Data Model](#data-model)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

Grevity is a full-featured, multi-company GST billing and inventory management web application built with ASP.NET Core MVC. It enables businesses to manage customers, suppliers, products (with Bill of Materials), sales invoices, purchase bills, and comprehensive financial reports — all within a multi-tenant architecture where each company's data is fully isolated. The application supports the complete document lifecycle from Draft through Invoice, automatic stock management, Indian GST compliance, and Excel import/export.

---

## Tech Stack

- **ASP.NET Core MVC** (.NET, C#)
- **Entity Framework Core** with SQL Server
- **Cookie Authentication** (8-hour sliding expiry)
- **HTMX** (`hx-boost` on `<body>` — smooth, SPA-like navigation without full page reloads)
- **NProgress** (loading bar on every page transition)
- **Bootstrap 5** (responsive UI framework with Material Symbols icons)
- **ClosedXML** (Excel `.xlsx` import and export)
- **ASP.NET Core Data Protection** (email password encryption at rest)
- **Indian Numbering System Helper** (Lakh/Crore formatting for invoice amount-in-words)

---

## Features

### 1. Authentication

User registration, login, password recovery via email OTP, and session management with 8-hour sliding cookie expiry.

#### How to Use

1. **Register a new account:**
   - Navigate to `/Account/Register`.
   - Fill in **Username**, **Email**, **Password**, and **Confirm Password**.
   - Click **Register** → A default company is automatically created and linked to your account.
   - You are redirected to the Login page.

2. **Log in:**
   - Navigate to `/Account/Login`.
   - Enter your **Username** and **Password**.
   - Click **Login** → You are taken to the Dashboard. Your session lasts 8 hours (sliding expiry).

3. **Forgot Password:**
   - On the Login page, click **Forgot Password**.
   - Enter your registered **Email** and click **Send OTP**.
   - Check your email for the OTP code.
   - On `/Account/VerifyOtp`, enter the **OTP** and click **Verify**.
   - On the Reset form, enter your **New Password** and **Confirm Password**, then click **Reset Password**.

4. **Logout:**
   - Click the **user pill dropdown** (top-right of the navbar showing "Hello, [Username]!").
   - Click **Logout** → A POST request clears the authentication cookie and you are redirected to Login.

---

### 2. Dashboard

The Dashboard (`/Home/Index`) provides a high-level financial overview of the active company with aggregate stat cards and quick-access shortcuts.

#### How to Use

1. After login, the **Dashboard** loads automatically.
2. View the summary cards at the top:
   - **Total Revenue** — sum of all invoice grand totals
   - **Total Invoices** — count of all invoices
   - **Customers** — total customer count
   - **Active Items** — total product count
3. Use the **Business Toolkit** quick-action buttons:
   - **Sales Invoice** → navigates to `/Invoice/Create`
   - **Purchase Bill** → navigates to `/Purchase/Create`
   - **Add Product** → navigates to `/Product/Create`
4. Under **Analytics Reports**, click:
   - **Detailed Sales Analysis** → navigates to `/Reports/SalesReport`
   - **Comprehensive Purchase Logs** → navigates to `/Reports/PurchaseReport`
   - **Global Financial Statement** → navigates to `/Reports/ConsolidatedReport`
5. In the Activity Stream panel, click **VIEW ALL LOGS** → navigates to `/AuditLogs/Index`.

---

### 3. Multi-Company Support

Grevity supports multiple companies per user. All entities (customers, suppliers, products, invoices, etc.) are automatically scoped to the active company via EF Core global query filters — no cross-company data leakage.

#### How to Use

1. **Switch company:**
   - In the top navbar, click the **Company Switcher** dropdown (visible on desktop).
   - Select the desired company name → The active company session is updated immediately; all subsequent pages show data for that company only.

2. **Add a new company:**
   - In the sidebar under **System**, click **Manage Companies** (`/Settings/List`).
   - Click **New Registration** → Fill in company details on the Settings form → Click **Save**.
   - The new company is linked to your user account.

3. **Edit a company:**
   - On `/Settings/List`, click the **Settings** icon on a company row → Edit the company profile on `/Settings/Index?id=N` → Click **Save**.

4. **Delete a company:**
   - On `/Settings/List`, click **Delete** on a company row → Confirm in the modal → The company and all associated data are removed.

---

### 4. Customer Management

Full CRUD management for customers with search, filtering, details view, and bulk Excel import. Customer data is scoped to the active company.

#### How to Use

1. **View all customers:**
   - Sidebar → **Master Data** → **Customers** (`/Customer/Index`).
   - Use the **search box** at the top to filter by name, mobile, or email in real time.

2. **Add a customer:**
   - Click the **Add New Customer** button.
   - Fill in **Name**, **Address**, **Mobile**, **Email**, **GSTIN** (if GST is enabled), and **Opening Balance**.
   - Click **Save**.

3. **Edit a customer:**
   - On the customer list, click the **Edit** (pencil) icon on a row → Update fields → Click **Save**.

4. **View details:**
   - Click the **View Details** (eye) icon → Opens `/Customer/Details/{id}` with full customer information.

5. **Delete a customer:**
   - Click the **Delete** (trash) icon → A confirmation modal appears → Click **Delete Now**.

6. **Import customers from Excel:**
   - On `/Customer/Index`, click the **Import** button.
   - On `/Customer/Import`, choose a `.xlsx` file with columns: Name, Mobile, Email, Address, GSTIN, Opening Balance.
   - Click **Upload** → Customers are bulk-inserted into the active company.

---

### 5. Supplier Management

Supplier management mirrors the customer module with full CRUD operations, search, and GST-aware fields. Suppliers represent vendors from whom you purchase goods.

#### How to Use

1. **View all suppliers:**
   - Sidebar → **Master Data** → **Suppliers** (`/Supplier/Index`).
   - Use the **search box** to filter by name, mobile, or email.

2. **Add a supplier:**
   - Click **Add New Supplier** → Fill in **Name**, **Address**, **Mobile**, **Email**, **GSTIN**, **Opening Balance** → Click **Save**.

3. **Edit a supplier:**
   - Click the **Edit** icon on the row → Update fields → Click **Save**.

4. **View details:**
   - Click the **View Details** icon → Opens `/Supplier/Details/{id}`.

5. **Delete a supplier:**
   - Click the **Delete** icon → Confirm in the modal.

---

### 6. Sub-Product Management

Sub-products represent raw materials or components used in the composition of finished products (Bill of Materials). They have independent stock tracking.

#### How to Use

1. **View all sub-products:**
   - Sidebar → **Master Data** → **Sub Products** (`/SubProduct/Index`).

2. **Add a sub-product:**
   - Click **Create New** → Fill in **Name**, **Description**, **Unit** (e.g., Kg, Pcs, Liter), **Current Stock**, **Purchase Price**, and **Low Stock Alert Limit** → Click **Save**.

3. **Edit a sub-product:**
   - Click **Edit** on a row → Update fields → Click **Save**.

4. **Delete a sub-product:**
   - Click **Delete** → Confirm in the modal.

> **Stock behaviour:** Sub-product stock is automatically increased when a Purchase Bill reaches the *Invoice* stage and decreased when a Sales Invoice at the *Invoice* stage is saved.

---

### 7. Product Management

Products are finished goods that can optionally be composed of sub-products (Bill of Materials). Each product has pricing, HSN code, GST rate, stock tracking, and an item type (Sales or Purchase).

#### How to Use

1. **View all products:**
   - Sidebar → **Master Data** → **Products** (`/Product/Index`).
   - Use the **search box** to filter by product name or HSN code.

2. **Add a product:**
   - Click **Add New Product**.
   - Fill in: **Name**, **Description**, **HSN Code**, **Unit**, **Sale Price**, **Purchase Price**, **GST %** (pre-filled from company default), **Stock Quantity**, **Low Stock Alert Limit**, and **Item Type** (Sales or Purchase).
   - In the **Sub-Product Composition** section, select sub-products and enter the required quantity for each.
   - Click **Save**.

3. **Edit a product:**
   - Click **Edit** on a row → Update product fields and sub-product composition → Click **Save**.

4. **View details:**
   - Click **View Details** → See product info, composition, and stock levels.

5. **Delete a product:**
   - Click **Delete** → Confirm in the modal.

> **Stock validation:** When creating a Sales Invoice, the system checks stock availability via the `CheckStockAvailability` JSON API (`/Product/CheckStockAvailability?id=N&quantity=Q`) before allowing the save. A `GetComposition` JSON API is also available for fetching product composition data.

---

### 8. Sales Invoice Management

Full document lifecycle management for sales invoices: **Draft → Quotation → Order → Invoice → (Cancelled)**. Supports GST calculation, discounts, round-off, payment tracking, and printable invoice generation.

#### How to Use

1. **View all sales invoices:**
   - Sidebar → **Transactions** → **Billing** → **Sales Invoices** (`/Invoice/Index`).
   - Use the **search box** to filter by customer name or invoice number.
   - Use stage filter buttons to narrow results by document stage.

2. **Create a sales invoice:**
   1. Click the **Create New Invoice** button.
   2. Select a **Customer**, set **Invoice Date** and **Due Date**.
   3. Choose the **Document Stage** (Draft, Quotation, Order, or Invoice).
   4. Add line items: select a **Product** → quantity and price auto-fill → GST is calculated automatically if GST is enabled in company settings.
   5. Adjust **Discount** and **Round-off** fields as needed.
   6. Click **Save** → An auto-generated invoice number (pattern: `{sequence}-{YY}`) is assigned.

3. **Edit an invoice:**
   - Click the **Edit** icon on the list → Modify fields → Click **Save**.

4. **View / Print an invoice:**
   - Click the **Print/View** (eye/printer) icon → Opens `/Invoice/Details/{id}`.
   - The detail page shows a printable invoice with company logo, customer details, line items, GST breakdown, and amount in words (Indian numbering system — Lakh/Crore).
   - Click the **Print** button → triggers `window.print()`.

5. **Advance the document stage (on Details page):**
   - If stage is **Quotation** → Click **Confirm Order** → stage moves to Order.
   - If stage is **Order** → Click **Generate Invoice** → stage moves to Invoice (stock is deducted at this point).

6. **Delete an invoice:**
   - Click **Delete** on the list → Confirm in the modal.

---

### 9. Purchase Bill Management

Purchase bill management mirrors sales invoices but for supplier purchases. Uses the same Invoice entity with `InvoiceType = "Purchase"`.

#### How to Use

1. **View all purchase bills:**
   - Sidebar → **Transactions** → **Billing** → **Purchase Bills** (`/Purchase/Index`).
   - Use the **search box** to filter by supplier name or bill number. Stage filter is also available.

2. **Create a purchase bill:**
   1. Click the **Create New Purchase** button.
   2. Select a **Supplier**, set **Invoice Date** and the **Document Stage**.
   3. Add line items with **Products** (filtered to Purchase type) or **Sub-Products**, quantities, prices, and GST.
   4. Click **Save** → Auto-generated bill number is assigned. Sub-product stock is increased when stage = Invoice.

3. **Edit a purchase bill:**
   - Click the **Edit** icon → Update fields → Click **Save**.

4. **View details:**
   - Click the view icon → Opens `/Purchase/Details/{id}` with full bill details and company/supplier information.

5. **Advance stage:**
   - On the Details page, stage-conversion buttons appear based on the current stage (same workflow as sales).

6. **Delete a purchase bill:**
   - Click **Delete** → Confirm in the modal.

---

### 10. Reports

Grevity includes a comprehensive suite of financial reports. All date-filtered reports support optional **From Date** and **To Date** filters — apply them by clicking the **Filter / Generate Report** button.

#### How to Use

| Report | Navigation | What It Shows |
|--------|-----------|---------------|
| **Sales Report** | Sidebar → **Analysis** → **Reports** → **Sales Report** (`/Reports/SalesReport`) | All sales invoices in the date range with totals |
| **Export Sales to Excel** | On the Sales Report page → Click **Export to Excel** | Downloads a `.xlsx` file of filtered sales data |
| **Purchase Report** | Sidebar → **Analysis** → **Reports** → **Purchase Report** (`/Reports/PurchaseReport`) | All purchase bills in the date range |
| **Unpaid Sales** | Sidebar → **Analysis** → **Reports** → **Unpaid Sales** (`/Reports/UnpaidSales`) | Sales invoices with outstanding balance > 0 |
| **Unpaid Purchase** | Sidebar → **Analysis** → **Reports** → **Unpaid Purchase** (`/Reports/UnpaidPurchase`) | Purchase bills with outstanding balance > 0 |
| **Paid Purchase** | Sidebar → **Analysis** → **Reports** → **Paid Purchase** (`/Reports/PaidPurchase`) | Fully paid purchase bills |
| **Customer Outstanding** | Sidebar → **Analysis** → **Reports** → **Customer Outstanding** (`/Reports/CustomerOutstanding`) | Customers with non-zero balance |
| **Consolidated Report** | Sidebar → **Analysis** → **Reports** → **Consolidated** (`/Reports/ConsolidatedReport`) | Global financial summary: total receivable, total payable, total sales, total purchases, top 5 debtors, top 5 creditors |

---

### 11. Settings / Company Profile

The Settings page allows you to configure your company's profile, GST preferences, email credentials (encrypted at rest), and logo.

#### How to Use

1. Navigate to: Sidebar → **System** → **Settings** (`/Settings/Index`), or via the user dropdown → **Settings**.
2. Fill in or update:
   - **Company Name**, **Address**, **City**, **State**, **Country**
   - **Phone**, **Mobile**, **Email**
   - **Email Password** (stored encrypted using ASP.NET Core Data Protection)
   - **GSTIN**
   - **Logo** (upload an image file — saved to `wwwroot/images/logos/`)
   - **GST Enabled** toggle
   - **Default GST %** (applied to new products)
   - **Current Financial Year** (e.g., `2024-2025`)
3. Click **Save** to persist changes.

---

### 12. Audit Logs

The audit log provides a chronological trail of all create, update, and delete actions performed within the active company.

#### How to Use

1. Navigate to: Sidebar → **System** → **Audit Logs** (`/AuditLogs/Index`).
2. The page displays the **last 100 audit entries** for the active company, showing:
   - **Timestamp** — when the action occurred
   - **User** — who performed the action
   - **Action** — Create, Update, or Delete
   - **Entity Name** — the type of entity affected (Product, Invoice, etc.)
   - **Entity ID** — the primary key of the affected record
3. Click on a row to expand the **Details** section (collapsible) showing the before/after data as JSON.

---

## Data Model

| Entity | Purpose |
|--------|---------|
| `User` | Authenticated user account (username, email, hashed password, OTP fields for password reset, role) |
| `BusinessSetting` | Company/tenant profile (name, address, GSTIN, logo, GST config, financial year) |
| `UserCompany` | Many-to-many link between users and companies (with default company flag) |
| `Customer` | Customer master data with name, contact info, GSTIN, opening and current balance |
| `Supplier` | Supplier master data with name, contact info, GSTIN, opening and current balance |
| `SubProduct` | Raw material / component with stock tracking, unit, purchase price, low stock alert |
| `Product` | Finished good with HSN code, GST %, sale/purchase price, stock, item type, and Bill of Materials composition |
| `ProductSubProductMapping` | Junction table linking products to sub-products with required quantity (Bill of Materials) |
| `Invoice` | Sales and purchase documents with document stage workflow, GST, payment tracking, auto-numbering |
| `InvoiceItem` | Line items within an invoice — linked to Product or SubProduct with quantity, price, GST, and snapshot fields |
| `PaymentTransaction` | Individual payment records against an invoice (amount, mode, reference number, notes) |
| `AuditLog` | System activity log per company (action, entity, timestamp, details JSON) |
| `DocumentStage` | Enum defining the document lifecycle: Draft → Quotation → Order → Invoice → Cancelled |
| `ICompanyEntity` | Interface marking entities that are company-scoped for multi-tenancy |
| `BaseEntity` | Abstract base class providing `Id`, `CreatedAt`, and `UpdatedAt` timestamps |

---

## Architecture

1. **Multi-Tenancy:** All data models implement `ICompanyEntity` with a `CompanyId` column. EF Core global query filters ensure each request only sees data for the active company stored in HTTP session (`ActiveCompanyId` via `ICompanyContext`).

2. **Repository Pattern:** Generic `IRepository<T>` interface (GetAll, GetById, Add, Update, Delete, Exists) implemented by `Repository<T>` using EF Core, registered as a scoped open generic.

3. **Service Layer:** Business logic resides in dedicated services — `AuthService`, `CustomerService`, `SupplierService`, `ProductService`, `SubProductService`, `InvoiceService`, `BusinessSettingService`, `EmailService`, and `CompanyContext` — each with a corresponding interface for dependency injection.

4. **Cookie Authentication:** ASP.NET Core cookie authentication with 8-hour sliding expiry. Login path is `/Account/Login`. Claims include username, role, and user ID.

5. **HTMX + NProgress:** `hx-boost="true"` on `<body>` intercepts all link navigations as AJAX requests (no full page reload). NProgress displays a loading bar during transitions.

6. **Auto Invoice Numbering:** Invoice numbers follow the pattern `{sequence}-{YY}`, scoped per company per financial year, generated server-side by `IInvoiceService.GenerateInvoiceNumberAsync()`.

7. **Stock Management:** Automatic stock deduction (sales) and increase (purchase) triggered only when the document stage transitions to `Invoice`. Stock validation is available via JSON API before save.

8. **Email OTP:** Forgot-password flow sends a time-limited OTP via SMTP (`IEmailService`). The email password stored in company settings is encrypted using ASP.NET Core Data Protection.

9. **Excel Import/Export:** ClosedXML is used for bulk customer import (`.xlsx` upload on `/Customer/Import`) and sales report export (download on `/Reports/ExportSalesInternal`).

10. **Session Management:** ASP.NET Core session middleware with 30-minute idle timeout, HTTP-only cookies, used primarily for storing the active company ID.

---

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version compatible with the project's target framework)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (LocalDB, Express, or full edition)

### Clone

```bash
git clone <repository-url>
cd Grevity
```

### Configure the Database

1. Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=GrevityDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

2. Apply EF Core migrations:

```bash
dotnet ef database update
```

### Run

```bash
dotnet run
```

Open your browser and navigate to `https://localhost:{port}` (the port is shown in the terminal output, or check `Properties/launchSettings.json`).

### First Steps

1. Register a new account at `/Account/Register`.
2. A default company is created automatically.
3. Configure your company profile under **Settings**.
4. Start adding customers, suppliers, products, and creating invoices.

---

## Project Structure

```
Grevity/
├── Controllers/             # MVC Controllers (Account, Home, Customer, Supplier,
│                            #   Product, SubProduct, Invoice, Purchase, Reports,
│                            #   Settings, AuditLogs)
├── Data/
│   └── AppDbContext.cs      # EF Core DbContext with global query filters
├── Helpers/
│   └── NumberToWords.cs     # Indian numbering system (Lakh/Crore) converter
├── Migrations/              # EF Core migration files
├── Models/
│   ├── Entities/            # Domain entities (User, Customer, Supplier, Product,
│   │                        #   SubProduct, Invoice, InvoiceItem, PaymentTransaction,
│   │                        #   BusinessSetting, AuditLog, UserCompany, etc.)
│   └── ViewModels/          # View-specific models (Dashboard, Login, Register,
│                            #   ForgotPassword, VerifyOtp, ResetPassword, Invoice)
├── Properties/
│   └── launchSettings.json  # Launch profiles and port configuration
├── Repositories/
│   ├── Interfaces/          # IRepository<T> generic interface
│   └── Implementations/     # Repository<T> EF Core implementation
├── Services/
│   ├── Interfaces/          # Service contracts (IAuthService, ICustomerService,
│   │                        #   IInvoiceService, IProductService, IEmailService, etc.)
│   └── Implementations/     # Service implementations with business logic
├── Views/
│   ├── Account/             # Login, Register, ForgotPassword, VerifyOtp, ResetPassword
│   ├── AuditLogs/           # Audit log listing
│   ├── Customer/            # Customer CRUD views + Import
│   ├── Home/                # Dashboard
│   ├── Invoice/             # Sales invoice CRUD + Details/Print
│   ├── Product/             # Product CRUD + Details
│   ├── Purchase/            # Purchase bill CRUD + Details
│   ├── Reports/             # Sales, Purchase, Unpaid, Paid, Outstanding, Consolidated
│   ├── Settings/            # Company profile + Company list
│   ├── Shared/              # _Layout, _Sidebar, _CompanySwitcher, _Notifications,
│   │                        #   _Breadcrumbs, delete confirmation modal
│   ├── SubProduct/          # Sub-product CRUD
│   └── Supplier/            # Supplier CRUD + Details
├── wwwroot/                 # Static files (CSS, JS, images, logos)
├── Program.cs               # Application entry point and DI configuration
├── appsettings.json         # Configuration (connection strings, etc.)
└── Grevity.sln              # Solution file
```

---

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/your-feature`).
3. Commit your changes (`git commit -m "Add your feature"`).
4. Push to the branch (`git push origin feature/your-feature`).
5. Open a Pull Request.

Please ensure your code follows the existing patterns (repository + service layer, multi-tenancy via `ICompanyEntity`) and includes appropriate error handling.

---

## License

This project is licensed under the [MIT License](LICENSE).
