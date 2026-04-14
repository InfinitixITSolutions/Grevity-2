# Grevity Bug Remediation Plan

## Summary

- Source review across controllers, services, data layer, and key Razor views plus `dotnet build Grevity.sln` found 11 concrete bugs or regressions.
- Fix order should be:
  1. tenancy and security
  2. destructive workflow correctness
  3. purchase-screen data integrity
  4. reporting and default-data issues

## Confirmed Bugs

### 1. Unauthorized company access

- Files:
  - `Controllers/SettingsController.cs`
- Impact:
  - Any authenticated user can read, update, or delete any company by guessing an `id`.
- Repro:
  1. Sign in as user A.
  2. Open `/Settings/Index?id=<companyBId>` for a company owned by user B.
  3. The page loads and allows edits.
  4. A crafted POST to `/Settings/Delete` with company B's id can also delete it.
- Fix:
  - Enforce `UserCompany` ownership before every settings read, save, and delete.
  - Reject unauthorized company ids with `Forbid()` or `NotFound()`.

### 2. Broken and unsafe delete flow

- Files:
  - `Views/Shared/_Layout.cshtml`
  - `Views/Settings/List.cshtml`
  - list views using `confirmDelete(...)`
- Impact:
  - The shared delete modal turns deletes into GET navigations.
  - Settings delete is broken because the server action is POST-only.
  - Other deletes are CSRF-prone and mutate state via GET.
- Repro:
  1. Open the company list.
  2. Click delete and confirm.
  3. The browser navigates to `GET /Settings/Delete?id=...`.
  4. The delete does not execute because the server action expects POST.
- Fix:
  - Replace delete links with POST forms.
  - Keep the modal as a submit helper, not a URL launcher.
  - Validate antiforgery tokens on the server.

### 3. Missing antiforgery protection on POST actions

- Files:
  - `Program.cs`
  - mutable controller actions
- Impact:
  - POST endpoints accept state changes without global antiforgery validation.
- Repro:
  1. Submit a forged cross-site POST to a mutable action like settings save or company switch.
  2. The server accepts it because there is no global antiforgery enforcement.
- Fix:
  - Register `AutoValidateAntiforgeryToken` globally for MVC.
  - Keep state-changing operations POST-only.

### 4. Invoice and purchase deletion leaves stock and balances corrupted

- Files:
  - `Services/Implementations/InvoiceService.cs`
- Impact:
  - Deleting an `Invoice`-stage document removes the header but does not reverse stock or customer and supplier balances.
- Repro:
  1. Create a sales invoice in `Invoice` stage.
  2. Note stock levels and customer current balance.
  3. Delete the invoice.
  4. Refresh reports or master data.
  5. Stock and balances remain adjusted.
- Fix:
  - Delete through a transaction that first reverts stock and balance effects, then deletes the invoice.

### 5. Purchase screens under-load supplier data

- Files:
  - `Services/Implementations/InvoiceService.cs`
  - `Controllers/PurchaseController.cs`
- Impact:
  - Purchase list and details operate on invoices that only include `Customer`, not `Supplier`.
  - Supplier-name search can fail and supplier info can render blank.
- Repro:
  1. Create a purchase bill.
  2. Open Purchase Index.
  3. Supplier cells are blank or supplier-name search does not match.
- Fix:
  - Include `Supplier` in purchase-facing invoice queries.
  - Prefer explicit sale and purchase query methods instead of one partially populated shared query.

### 6. Purchase edit rewrites historical prices and GST

- Files:
  - `Views/Purchase/Edit.cshtml`
- Impact:
  - Opening edit replaces saved line values with the current product or sub-product catalog values before the user changes anything.
- Repro:
  1. Create a purchase with an older unit price.
  2. Later update the product purchase price.
  3. Reopen the purchase in Edit.
  4. The form overwrites the saved line price and GST values on load.
- Fix:
  - Preserve persisted line-item values on initial render.
  - Only refresh from catalog values when the selected item actually changes.

### 7. User-entered invoice date is ignored on create

- Files:
  - `Services/Implementations/InvoiceService.cs`
- Impact:
  - Create always replaces the submitted `InvoiceDate` with `DateTime.Now`.
- Repro:
  1. Create a backdated sale or purchase.
  2. Save it.
  3. The saved document shows today's date instead of the chosen date.
- Fix:
  - Respect the submitted date and only default it when it is missing.

### 8. Dashboard total sales includes non-sales and non-finalized documents

- Files:
  - `Controllers/HomeController.cs`
- Impact:
  - Purchases, quotations, orders, and drafts inflate the dashboard sales figure.
- Repro:
  1. Create a purchase bill or a quotation.
  2. Open the dashboard.
  3. Total sales increases even though no finalized sale occurred.
- Fix:
  - Sum only sales invoices with `InvoiceType == "Sale"` and `Stage == Invoice`.

### 9. Company deletion leaves sub-products behind

- Files:
  - `Services/Implementations/BusinessSettingService.cs`
- Impact:
  - Deleting a company removes products and mappings but never removes `SubProducts`, leaving orphaned company-scoped inventory rows.
- Repro:
  1. Create sub-products for a company.
  2. Delete that company.
  3. Inspect the database.
  4. Sub-product rows remain.
- Fix:
  - Delete company-scoped `SubProducts` as part of the purge.
  - Keep mapping and delete ordering explicit.

### 10. Imported opening balances do not affect current balances

- Files:
  - `Controllers/CustomerController.cs`
  - `Services/Implementations/CustomerService.cs`
- Impact:
  - Imported customers can have `OpeningBalance > 0` while `CurrentBalance` stays `0`, so outstanding reports are wrong.
- Repro:
  1. Import a customer with opening balance `500`.
  2. Open Customer Outstanding or Consolidated Report.
  3. The receivable does not include that imported opening balance.
- Fix:
  - Initialize `CurrentBalance = OpeningBalance` on import and on normal create when current balance is not explicitly set.

### 11. New user registration seeds a stale financial year

- Files:
  - `Controllers/AccountController.cs`
- Impact:
  - Every newly created default company starts with hard-coded year `2024-2025`.
- Repro:
  1. Register a new user in April 2026.
  2. Open the default company settings.
  3. `CurrentFinancialYear` is still `2024-2025`.
- Fix:
  - Compute the financial year dynamically from the current date.

## Implementation Changes

- Add a shared ownership guard for company access and use it in settings read, write, and delete flows.
- Replace URL-based deletes with POST form submission plus antiforgery validation.
- Register global antiforgery validation for MVC.
- Refactor invoice querying so purchase screens load `Supplier` and sales screens load `Customer` intentionally.
- Centralize invoice side-effect reversal for delete and stage transitions.
- Preserve persisted line values on edit load.
- Respect submitted invoice dates.
- Normalize financial defaults:
  - current balance starts from opening balance
  - default financial year is computed dynamically

## Test Plan

- `dotnet build Grevity.sln`
- Verify one user cannot read, save, or delete another user's company by altering ids in query string or form post.
- Verify delete actions work from the UI and remain POST-only.
- Create, edit, delete, and stage-convert both sales and purchases while checking stock and balances before and after each step.
- Change a product purchase price after recording a purchase, then reopen Purchase Edit and confirm the old line prices remain unchanged.
- Create backdated sales and purchases and confirm the saved `InvoiceDate` matches the submitted date.
- Confirm Purchase Index shows supplier names and supplier-name search works.
- Confirm dashboard total sales ignores purchases and non-invoice stages.
- Delete a company with sub-products and verify no company-scoped inventory rows remain.
- Import customers with opening balances and verify reports use the imported amount.

## Notes

- Findings are based on source inspection and a successful build.
- There is no automated test project in the repo right now, so the repro paths above are documented but not executed end-to-end here.
