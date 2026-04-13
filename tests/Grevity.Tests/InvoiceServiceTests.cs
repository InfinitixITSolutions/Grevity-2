using Grevity.Models.Entities;
using Grevity.Repositories.Implementations;
using Grevity.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Grevity.Tests;

public class InvoiceServiceTests
{
    [Fact]
    public async Task DeleteInvoiceAsync_reverts_stock_and_customer_balance_for_finalized_sales()
    {
        using var connection = TestDb.CreateOpenConnection();
        var companyContext = new TestCompanyContext();
        companyContext.SetCurrentCompany(1);

        await using (var setupContext = TestDb.CreateContext(connection, companyContext))
        {
            setupContext.Products.Add(new Product
            {
                Id = 10,
                CompanyId = 1,
                Name = "Widget",
                StockQuantity = 8,
                Price = 50
            });
            setupContext.Customers.Add(new Customer
            {
                Id = 20,
                CompanyId = 1,
                Name = "Alice",
                Mobile = "1234567890",
                CurrentBalance = 80
            });
            setupContext.Invoices.Add(new Invoice
            {
                Id = 30,
                CompanyId = 1,
                CustomerId = 20,
                InvoiceType = "Sale",
                Stage = DocumentStage.Invoice,
                GrandTotal = 100,
                PaidAmount = 20,
                Status = "Partial Paid",
                InvoiceDate = new DateTime(2025, 1, 1),
                InvoiceItems =
                {
                    new InvoiceItem
                    {
                        Id = 31,
                        CompanyId = 1,
                        ProductId = 10,
                        ProductName = "Widget",
                        Quantity = 2,
                        UnitPrice = 50,
                        GSTPercentage = 0
                    }
                }
            });

            await setupContext.SaveChangesAsync();
        }

        await using (var verificationContext = TestDb.CreateContext(connection, companyContext))
        {
            var service = CreateInvoiceService(verificationContext);

            await service.DeleteInvoiceAsync(30);
        }

        await using var assertContext = TestDb.CreateContext(connection, companyContext);
        var product = await assertContext.Products.SingleAsync(p => p.Id == 10);
        var customer = await assertContext.Customers.SingleAsync(c => c.Id == 20);

        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(0, customer.CurrentBalance);
        Assert.False(await assertContext.Invoices.AnyAsync(i => i.Id == 30));
    }

    [Fact]
    public async Task CreateInvoiceAsync_preserves_user_supplied_invoice_date()
    {
        using var connection = TestDb.CreateOpenConnection();
        var companyContext = new TestCompanyContext();
        companyContext.SetCurrentCompany(1);

        await using (var setupContext = TestDb.CreateContext(connection, companyContext))
        {
            setupContext.Products.Add(new Product
            {
                Id = 10,
                CompanyId = 1,
                Name = "Widget",
                Price = 25
            });
            await setupContext.SaveChangesAsync();
        }

        var expectedDate = new DateTime(2024, 12, 31);

        await using var context = TestDb.CreateContext(connection, companyContext);
        var service = CreateInvoiceService(context);

        var invoice = await service.CreateInvoiceAsync(new Invoice
        {
            CompanyId = 1,
            InvoiceType = "Sale",
            Stage = DocumentStage.Draft,
            InvoiceDate = expectedDate,
            InvoiceItems =
            {
                new InvoiceItem
                {
                    ProductId = 10,
                    Quantity = 2,
                    UnitPrice = 25,
                    GSTPercentage = 0
                }
            }
        });

        Assert.Equal(expectedDate, invoice.InvoiceDate);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_includes_supplier_for_purchase_documents()
    {
        using var connection = TestDb.CreateOpenConnection();
        var companyContext = new TestCompanyContext();
        companyContext.SetCurrentCompany(1);

        await using (var setupContext = TestDb.CreateContext(connection, companyContext))
        {
            setupContext.Suppliers.Add(new Supplier
            {
                Id = 50,
                CompanyId = 1,
                Name = "Vendor",
                Mobile = "1234567890"
            });
            setupContext.Invoices.Add(new Invoice
            {
                Id = 60,
                CompanyId = 1,
                SupplierId = 50,
                InvoiceType = "Purchase",
                Stage = DocumentStage.Order,
                InvoiceDate = new DateTime(2025, 2, 1)
            });
            await setupContext.SaveChangesAsync();
        }

        await using var context = TestDb.CreateContext(connection, companyContext);
        var service = CreateInvoiceService(context);

        var invoice = (await service.GetAllInvoicesAsync()).Single(i => i.Id == 60);

        Assert.NotNull(invoice.Supplier);
        Assert.Equal("Vendor", invoice.Supplier!.Name);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_includes_supplier_for_purchase_documents()
    {
        using var connection = TestDb.CreateOpenConnection();
        var companyContext = new TestCompanyContext();
        companyContext.SetCurrentCompany(1);

        await using (var setupContext = TestDb.CreateContext(connection, companyContext))
        {
            setupContext.Suppliers.Add(new Supplier
            {
                Id = 70,
                CompanyId = 1,
                Name = "Vendor",
                Mobile = "1234567890"
            });
            setupContext.Invoices.Add(new Invoice
            {
                Id = 80,
                CompanyId = 1,
                SupplierId = 70,
                InvoiceType = "Purchase",
                Stage = DocumentStage.Order,
                InvoiceDate = new DateTime(2025, 2, 1)
            });
            await setupContext.SaveChangesAsync();
        }

        await using var context = TestDb.CreateContext(connection, companyContext);
        var service = CreateInvoiceService(context);

        var invoice = await service.GetInvoiceByIdAsync(80);

        Assert.NotNull(invoice);
        Assert.NotNull(invoice.Supplier);
        Assert.Equal("Vendor", invoice.Supplier!.Name);
    }

    private static InvoiceService CreateInvoiceService(Grevity.Data.AppDbContext context)
    {
        return new InvoiceService(
            new Repository<Invoice>(context),
            new Repository<Product>(context),
            new Repository<Customer>(context),
            new Repository<Supplier>(context),
            context);
    }
}
