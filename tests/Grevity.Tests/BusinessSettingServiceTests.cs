using Grevity.Models.Entities;
using Grevity.Repositories.Implementations;
using Grevity.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Grevity.Tests;

public class BusinessSettingServiceTests
{
    [Fact]
    public async Task DeleteSettingsAsync_removes_company_subproducts()
    {
        using var connection = TestDb.CreateOpenConnection();
        var companyContext = new TestCompanyContext();
        companyContext.SetCurrentCompany(1);

        await using (var setupContext = TestDb.CreateContext(connection, companyContext))
        {
            setupContext.BusinessSettings.Add(new BusinessSetting
            {
                Id = 1,
                CompanyName = "Acme",
                CurrentFinancialYear = "2025-2026"
            });
            setupContext.Products.Add(new Product
            {
                Id = 10,
                CompanyId = 1,
                Name = "Widget"
            });
            setupContext.SubProducts.Add(new SubProduct
            {
                Id = 11,
                CompanyId = 1,
                Name = "Steel",
                Unit = "Kg"
            });
            setupContext.ProductSubProductMappings.Add(new ProductSubProductMapping
            {
                Id = 12,
                CompanyId = 1,
                ProductId = 10,
                SubProductId = 11,
                RequiredQuantity = 2
            });
            await setupContext.SaveChangesAsync();
        }

        await using (var deleteContext = TestDb.CreateContext(connection, companyContext))
        {
            var service = new BusinessSettingService(new Repository<BusinessSetting>(deleteContext), companyContext, deleteContext);
            await service.DeleteSettingsAsync(1);
        }

        await using var assertContext = TestDb.CreateContext(connection, companyContext);
        Assert.Empty(await assertContext.SubProducts.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await assertContext.ProductSubProductMappings.IgnoreQueryFilters().ToListAsync());
    }
}
