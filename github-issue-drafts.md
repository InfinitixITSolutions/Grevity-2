# GitHub Issue Drafts

These drafts are ready to turn into GitHub issues manually or through automation later.

---

## 1. Lock down company settings access by ownership

**Title**

`Settings endpoints allow unauthorized cross-company read/update/delete by guessed id`

**Body**

### Problem

`SettingsController` trusts the incoming company id and does not verify that the current user is linked to that company through `UserCompany`.

### Impact

- Any authenticated user can read another company's settings.
- Any authenticated user can submit updates for another company.
- Any authenticated user can delete another company if they can post the id.

### Reproduction

1. Sign in as user A.
2. Find or guess a company id that belongs to user B.
3. Open `/Settings/Index?id=<otherId>`.
4. Observe that the other company's data loads.
5. Submit a POST to update or delete that company id.

### Expected

Only companies linked to the current user should be readable or mutable.

### Proposed fix

- Add a shared ownership check using `UserCompany`.
- Apply it to settings GET, settings POST, and delete.
- Return `Forbid()` or `NotFound()` for unauthorized ids.

---

## 2. Replace GET-based delete confirmation with POST-backed deletes

**Title**

`Shared delete modal converts destructive actions into GET navigations`

**Body**

### Problem

The shared `confirmDelete` helper opens a modal and writes a URL into an anchor. That turns delete confirmations into GET requests.

### Impact

- Company delete is broken because the action is POST-only.
- Other deletes are unsafe and CSRF-prone because they mutate state via GET.

### Reproduction

1. Open a list page with delete actions.
2. Click delete and confirm in the modal.
3. Observe that the browser navigates to a delete URL instead of submitting a POST form.
4. On the company list, the delete fails because the action only accepts POST.

### Expected

All destructive actions should submit POST forms with antiforgery protection.

### Proposed fix

- Change modal logic to submit a form instead of navigating to a URL.
- Update delete buttons to use forms.
- Keep all delete actions POST-only.

---

## 3. Enable antiforgery validation for mutable POST actions

**Title**

`POST endpoints are missing global antiforgery enforcement`

**Body**

### Problem

MVC is registered without global antiforgery validation, and mutable POST actions do not consistently enforce antiforgery tokens.

### Impact

- Cross-site request forgery is possible for settings save, company switch, and other mutable actions.

### Reproduction

1. While logged in, submit a forged POST from another site to a mutable endpoint.
2. Observe that the application accepts the request.

### Expected

All mutable POST actions should reject requests without a valid antiforgery token.

### Proposed fix

- Register `AutoValidateAntiforgeryToken` globally.
- Keep mutations on POST-only endpoints.

---

## 4. Revert stock and balances when deleting finalized documents

**Title**

`Deleting Invoice-stage documents leaves stock and current balances out of sync`

**Body**

### Problem

`DeleteInvoiceAsync` deletes the invoice record without reversing stock movements or customer and supplier balance effects.

### Impact

- Inventory remains overstated or understated after deletion.
- Outstanding balances remain incorrect.
- Reports drift from source documents.

### Reproduction

1. Create a sale or purchase in `Invoice` stage.
2. Record the related stock and current balance values.
3. Delete the document.
4. Observe that the document is gone but stock and balances remain changed.

### Expected

Deleting a finalized document should fully reverse its side effects before deletion.

### Proposed fix

- Wrap deletion in a transaction.
- Reverse stock and balances first.
- Delete the invoice only after reversal succeeds.

---

## 5. Load supplier data explicitly for purchase screens

**Title**

`Purchase list and details use invoice queries that do not include Supplier`

**Body**

### Problem

The shared invoice query methods include `Customer`, but purchase screens depend on `Supplier`.

### Impact

- Supplier columns render blank.
- Supplier-name search can fail.
- Purchase details have incomplete vendor information.

### Reproduction

1. Create a purchase bill.
2. Open Purchase Index.
3. Search using the supplier name.
4. Observe that supplier-related UI is incomplete or search does not match.

### Expected

Purchase pages should load supplier navigation data consistently.

### Proposed fix

- Include `Supplier` in purchase queries.
- Split shared sale and purchase query methods if needed.

---

## 6. Preserve historical line values when editing purchases

**Title**

`Purchase Edit overwrites saved line prices and GST from current catalog values on page load`

**Body**

### Problem

The Purchase Edit view replays client-side item selection logic during initial load, which replaces saved `UnitPrice` and `GSTPercentage` with current product or sub-product defaults.

### Impact

- Historical purchase records lose original economics when edited.
- Users can accidentally resave altered numbers without changing the item intentionally.

### Reproduction

1. Create a purchase with a specific old price.
2. Change the catalog purchase price for that item.
3. Reopen the purchase in Edit.
4. Observe that the line price is replaced before any user change.

### Expected

Existing line items should preserve their saved price and GST values on initial load.

### Proposed fix

- Render persisted values as the initial state.
- Only refresh from catalog defaults when the selected item actually changes.

---

## 7. Respect submitted invoice date on create

**Title**

`Create flow overwrites the user-selected InvoiceDate with current date`

**Body**

### Problem

`CreateInvoiceAsync` forces `InvoiceDate = DateTime.Now` even when the user submitted a different date.

### Impact

- Backdated documents cannot be created correctly.
- Reports and financial periods become inaccurate.

### Reproduction

1. Create a sale or purchase with a past date.
2. Save the document.
3. Observe that the saved record uses today's date instead.

### Expected

The submitted invoice date should be preserved.

### Proposed fix

- Only default the date when it is missing.

---

## 8. Restrict dashboard sales total to finalized sales invoices

**Title**

`Dashboard TotalSales includes purchases and non-finalized documents`

**Body**

### Problem

The home dashboard totals all invoices regardless of `InvoiceType` or `Stage`.

### Impact

- Purchases inflate sales metrics.
- Quotations, orders, and drafts appear as real revenue.

### Reproduction

1. Create a purchase or a quotation.
2. Open the dashboard.
3. Observe that total sales increases.

### Expected

Dashboard sales should count only finalized sales invoices.

### Proposed fix

- Filter to `InvoiceType == "Sale"` and `Stage == Invoice`.

---

## 9. Delete company-scoped sub-products during company purge

**Title**

`Company deletion leaves orphaned SubProducts in the database`

**Body**

### Problem

The company purge deletes invoices, payments, products, customers, and suppliers but does not remove `SubProducts`.

### Impact

- Company deletion leaves orphaned inventory data.
- Future reporting or maintenance queries can become inconsistent.

### Reproduction

1. Create one or more sub-products for a company.
2. Delete that company.
3. Inspect the database.
4. Observe that sub-product rows still exist.

### Expected

Deleting a company should remove all company-scoped inventory entities.

### Proposed fix

- Include `SubProducts` in the company purge.
- Keep delete order explicit around mappings and related entities.

---

## 10. Imported opening balances should initialize current balances

**Title**

`Customer import stores OpeningBalance without updating CurrentBalance`

**Body**

### Problem

The import path sets `OpeningBalance` but leaves `CurrentBalance` at its default.

### Impact

- Outstanding reports understate receivables.
- Imported customer ledgers start in the wrong state.

### Reproduction

1. Import a customer with opening balance `500`.
2. Open the outstanding or consolidated report.
3. Observe that the imported balance is missing from current outstanding values.

### Expected

Imported opening balance should be reflected in the current balance unless explicitly overridden.

### Proposed fix

- Initialize `CurrentBalance = OpeningBalance` during import and standard customer creation.

---

## 11. Compute default financial year dynamically for new companies

**Title**

`New user registration seeds a hard-coded and outdated CurrentFinancialYear`

**Body**

### Problem

Registration creates a default company with hard-coded year `2024-2025`.

### Impact

- New companies start with stale financial metadata.
- Users must correct settings manually after registration.

### Reproduction

1. Register a new user after March 2025.
2. Open the default company settings.
3. Observe that `CurrentFinancialYear` is still `2024-2025`.

### Expected

The default financial year should match the current business period.

### Proposed fix

- Compute the current financial year dynamically at registration time.
