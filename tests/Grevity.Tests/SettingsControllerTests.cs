using System.Security.Claims;
using Grevity.Controllers;
using Grevity.Models.Entities;
using Grevity.Repositories.Implementations;
using Grevity.Services.Implementations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace Grevity.Tests;

public class SettingsControllerTests
{
    [Fact]
    public async Task Index_get_for_foreign_company_returns_forbid()
    {
        using var connection = TestDb.CreateOpenConnection();
        var companyContext = new TestCompanyContext();
        companyContext.SetCurrentCompany(1);

        await using (var setupContext = TestDb.CreateContext(connection, companyContext))
        {
            setupContext.Users.Add(new User
            {
                Id = 42,
                Username = "tester",
                Email = "tester@example.com",
                PasswordHash = "pw"
            });
            setupContext.BusinessSettings.AddRange(
                new BusinessSetting { Id = 1, CompanyName = "Owned", CurrentFinancialYear = "2025-2026" },
                new BusinessSetting { Id = 2, CompanyName = "Foreign", CurrentFinancialYear = "2025-2026" });
            setupContext.UserCompanies.Add(new UserCompany { UserId = 42, BusinessSettingId = 1, IsDefault = true });
            await setupContext.SaveChangesAsync();
        }

        await using var context = TestDb.CreateContext(connection, companyContext);
        var controller = CreateController(context, companyContext);
        controller.SetUser(new Claim("UserId", "42"));

        var result = await controller.Index(2);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Index_post_for_foreign_company_returns_forbid()
    {
        using var connection = TestDb.CreateOpenConnection();
        var companyContext = new TestCompanyContext();
        companyContext.SetCurrentCompany(1);

        await using (var setupContext = TestDb.CreateContext(connection, companyContext))
        {
            setupContext.Users.Add(new User
            {
                Id = 42,
                Username = "tester",
                Email = "tester@example.com",
                PasswordHash = "pw"
            });
            setupContext.BusinessSettings.AddRange(
                new BusinessSetting { Id = 1, CompanyName = "Owned", CurrentFinancialYear = "2025-2026" },
                new BusinessSetting { Id = 2, CompanyName = "Foreign", CurrentFinancialYear = "2025-2026" });
            setupContext.UserCompanies.Add(new UserCompany { UserId = 42, BusinessSettingId = 1, IsDefault = true });
            await setupContext.SaveChangesAsync();
        }

        await using var context = TestDb.CreateContext(connection, companyContext);
        var controller = CreateController(context, companyContext);
        controller.SetUser(new Claim("UserId", "42"));
        controller.SetTempData();

        var result = await controller.Index(new BusinessSetting
        {
            Id = 2,
            CompanyName = "Foreign Updated",
            CurrentFinancialYear = "2025-2026"
        }, null);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_for_foreign_company_returns_forbid_without_removing_company()
    {
        using var connection = TestDb.CreateOpenConnection();
        var companyContext = new TestCompanyContext();
        companyContext.SetCurrentCompany(1);

        await using (var setupContext = TestDb.CreateContext(connection, companyContext))
        {
            setupContext.Users.Add(new User
            {
                Id = 42,
                Username = "tester",
                Email = "tester@example.com",
                PasswordHash = "pw"
            });
            setupContext.BusinessSettings.AddRange(
                new BusinessSetting { Id = 1, CompanyName = "Owned", CurrentFinancialYear = "2025-2026" },
                new BusinessSetting { Id = 2, CompanyName = "Foreign", CurrentFinancialYear = "2025-2026" });
            setupContext.UserCompanies.Add(new UserCompany { UserId = 42, BusinessSettingId = 1, IsDefault = true });
            await setupContext.SaveChangesAsync();
        }

        await using var context = TestDb.CreateContext(connection, companyContext);
        var controller = CreateController(context, companyContext);
        controller.SetUser(new Claim("UserId", "42"));
        controller.SetTempData();

        var result = await controller.Delete(2);

        Assert.IsType<ForbidResult>(result);
        Assert.NotNull(await context.BusinessSettings.FindAsync(2));
    }

    private static SettingsController CreateController(Grevity.Data.AppDbContext context, TestCompanyContext companyContext)
    {
        var providerDirectory = Path.Combine(Path.GetTempPath(), "GrevityTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(providerDirectory);

        return new SettingsController(
            new BusinessSettingService(new Repository<BusinessSetting>(context), companyContext, context),
            new TestWebHostEnvironment(),
            DataProtectionProvider.Create(new DirectoryInfo(providerDirectory)),
            new Repository<UserCompany>(context),
            companyContext);
    }
}
