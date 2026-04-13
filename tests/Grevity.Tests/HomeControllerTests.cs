using Grevity.Controllers;
using Grevity.Models.Entities;
using Grevity.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Grevity.Tests;

public class HomeControllerTests
{
    [Fact]
    public async Task Index_counts_only_finalized_sales_in_dashboard_totals()
    {
        var controller = new HomeController(
            new StaticCustomerService(new[]
            {
                new Customer { Id = 1, Name = "Alice", Mobile = "1234567890" },
                new Customer { Id = 2, Name = "Bob", Mobile = "1234567891" }
            }),
            new StaticProductService(new[]
            {
                new Product { Id = 1, Name = "A" },
                new Product { Id = 2, Name = "B" },
                new Product { Id = 3, Name = "C" }
            }),
            new StaticInvoiceService(new[]
            {
                new Invoice { Id = 1, InvoiceType = "Sale", Stage = DocumentStage.Invoice, GrandTotal = 100 },
                new Invoice { Id = 2, InvoiceType = "Sale", Stage = DocumentStage.Draft, GrandTotal = 50 },
                new Invoice { Id = 3, InvoiceType = "Purchase", Stage = DocumentStage.Invoice, GrandTotal = 70 }
            }));

        var result = await controller.Index();
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

        Assert.Equal(2, model.TotalCustomers);
        Assert.Equal(3, model.TotalProducts);
        Assert.Equal(1, model.TotalInvoices);
        Assert.Equal(100, model.TotalSales);
    }
}
