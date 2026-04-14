using Grevity.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Grevity.Tests;

public class SourceRegressionTests
{
    [Fact]
    public void Program_enforces_antiforgery_globally()
    {
        var source = TestPaths.ReadRepoFile("Program.cs");

        Assert.Contains("AddControllersWithViews(options =>", source);
        Assert.Contains("AutoValidateAntiforgeryTokenAttribute", source);
    }

    [Fact]
    public void Layout_uses_post_form_for_delete_confirmation()
    {
        var source = TestPaths.ReadRepoFile(Path.Combine("Views", "Shared", "_Layout.cshtml"));

        Assert.Contains("<form id=\"deleteConfirmForm\" method=\"post\"", source);
        Assert.Contains("@Html.AntiForgeryToken()", source);
        Assert.Contains("document.getElementById('deleteConfirmForm').action = url;", source);
        Assert.DoesNotContain("confirmDeleteButton", source);
    }

    [Theory]
    [InlineData(typeof(CustomerController))]
    [InlineData(typeof(SupplierController))]
    [InlineData(typeof(ProductController))]
    [InlineData(typeof(SubProductController))]
    [InlineData(typeof(InvoiceController))]
    [InlineData(typeof(PurchaseController))]
    public void Delete_actions_require_http_post(Type controllerType)
    {
        var method = controllerType.GetMethod("Delete");

        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true), attribute => attribute is HttpPostAttribute);
    }

    [Fact]
    public void Customer_controller_syncs_current_balance_from_opening_balance()
    {
        var source = TestPaths.ReadRepoFile(Path.Combine("Controllers", "CustomerController.cs"));

        Assert.Contains("CurrentBalance = row.Cell(6).GetValue<decimal>()", source);
        Assert.Contains("customer.CurrentBalance == 0 && customer.OpeningBalance != 0", source);
    }

    [Fact]
    public void Purchase_edit_preserves_existing_prices_on_initial_load()
    {
        var source = TestPaths.ReadRepoFile(Path.Combine("Views", "Purchase", "Edit.cshtml"));

        Assert.Contains("function productChanged(select, index, isInitialLoad = false)", source);
        Assert.Contains("if (!isInitialLoad)", source);
        Assert.Contains("productChanged(selectEl, itemIndex, existingItem != null);", source);
    }

    [Theory]
    [InlineData("Views/Invoice/Create.cshtml")]
    [InlineData("Views/Invoice/Edit.cshtml")]
    [InlineData("Views/Purchase/Create.cshtml")]
    [InlineData("Views/Purchase/Edit.cshtml")]
    public void Billing_pages_reinitialize_after_htmx_swaps(string relativePath)
    {
        var source = TestPaths.ReadRepoFile(relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.Contains("function initPage()", source);
        Assert.Contains("window.onload = initPage;", source);
        Assert.Contains("document.addEventListener('htmx:afterSwap', initPage);", source);
    }

    [Fact]
    public void Account_registration_uses_dynamic_financial_year()
    {
        var source = TestPaths.ReadRepoFile(Path.Combine("Controllers", "AccountController.cs"));

        Assert.Contains("DateTime.Now.Month >= 4 ? DateTime.Now.Year : DateTime.Now.Year - 1", source);
        Assert.DoesNotContain("CurrentFinancialYear = \"2024-2025\"", source);
    }
}
