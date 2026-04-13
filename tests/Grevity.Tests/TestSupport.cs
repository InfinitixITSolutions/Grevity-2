using System.Security.Claims;
using Grevity.Data;
using Grevity.Models.Entities;
using Grevity.Repositories.Interfaces;
using Grevity.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace Grevity.Tests;

internal static class TestPaths
{
    private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

    public static string RepoRoot => RepoRootLazy.Value;

    public static string ReadRepoFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepoRoot, relativePath));
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Grevity.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

internal sealed class TestCompanyContext : ICompanyContext
{
    public int? CurrentCompanyId { get; private set; }

    public Task SetCompanyAsync(int companyId)
    {
        CurrentCompanyId = companyId;
        return Task.CompletedTask;
    }

    public void SetCurrentCompany(int? companyId)
    {
        CurrentCompanyId = companyId;
    }
}

internal sealed class TestTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object>();
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
    }
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Grevity.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "GrevityTestsWebRoot");
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = TestPaths.RepoRoot;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal static class TestDb
{
    public static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    public static AppDbContext CreateContext(SqliteConnection connection, TestCompanyContext companyContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };

        var context = new AppDbContext(options, companyContext, httpContextAccessor);
        context.Database.EnsureCreated();
        return context;
    }
}

internal sealed class StaticCustomerService : ICustomerService
{
    private readonly IEnumerable<Customer> _customers;

    public StaticCustomerService(IEnumerable<Customer> customers)
    {
        _customers = customers;
    }

    public Task<IEnumerable<Customer>> GetAllCustomersAsync() => Task.FromResult(_customers);
    public Task<Customer> GetCustomerByIdAsync(int id) => Task.FromResult(_customers.First(c => c.Id == id));
    public Task AddCustomerAsync(Customer customer) => throw new NotSupportedException();
    public Task UpdateCustomerAsync(Customer customer) => throw new NotSupportedException();
    public Task DeleteCustomerAsync(int id) => throw new NotSupportedException();
}

internal sealed class StaticProductService : IProductService
{
    private readonly IEnumerable<Product> _products;

    public StaticProductService(IEnumerable<Product> products)
    {
        _products = products;
    }

    public Task<IEnumerable<Product>> GetAllProductsAsync() => Task.FromResult(_products);
    public Task<Product> GetProductByIdAsync(int id) => Task.FromResult(_products.First(p => p.Id == id));
    public Task AddProductAsync(Product product) => throw new NotSupportedException();
    public Task UpdateProductAsync(Product product) => throw new NotSupportedException();
    public Task DeleteProductAsync(int id) => throw new NotSupportedException();
    public Task<IEnumerable<ProductSubProductMapping>> GetProductCompositionAsync(int productId) => throw new NotSupportedException();
    public Task UpdateProductCompositionAsync(int productId, IEnumerable<ProductSubProductMapping> mappings) => throw new NotSupportedException();
    public Task<bool> ValidateStockForSaleAsync(int productId, decimal saleQuantity) => throw new NotSupportedException();
    public Task<string> GetStockValidationMessageAsync(int productId, decimal saleQuantity) => throw new NotSupportedException();
}

internal sealed class StaticInvoiceService : IInvoiceService
{
    private readonly IEnumerable<Invoice> _invoices;

    public StaticInvoiceService(IEnumerable<Invoice> invoices)
    {
        _invoices = invoices;
    }

    public Task<IEnumerable<Invoice>> GetAllInvoicesAsync() => Task.FromResult(_invoices);
    public Task<Invoice> GetInvoiceByIdAsync(int id) => Task.FromResult(_invoices.First(i => i.Id == id));
    public Task<Invoice> CreateInvoiceAsync(Invoice invoice) => throw new NotSupportedException();
    public Task<Invoice> UpdateInvoiceAsync(Invoice invoice) => throw new NotSupportedException();
    public Task<string> GenerateInvoiceNumberAsync() => throw new NotSupportedException();
    public Task AddPaymentTransactionAsync(int invoiceId, PaymentTransaction transaction) => throw new NotSupportedException();
    public Task<IEnumerable<PaymentTransaction>> GetPaymentTransactionsAsync(int invoiceId) => throw new NotSupportedException();
    public Task DeleteInvoiceAsync(int id) => throw new NotSupportedException();
    public Task<Invoice> UpdateStageAsync(int invoiceId, DocumentStage newStage) => throw new NotSupportedException();
    public Task UpdatePaymentAsync(int invoiceId, decimal newPaidAmount) => throw new NotSupportedException();
}

internal sealed class StaticBusinessSettingService : IBusinessSettingService
{
    private readonly IEnumerable<BusinessSetting> _settings;

    public StaticBusinessSettingService(IEnumerable<BusinessSetting>? settings = null)
    {
        _settings = settings ?? Array.Empty<BusinessSetting>();
    }

    public Task<BusinessSetting> GetSettingsAsync(int? id = null, bool asNoTracking = false)
    {
        return Task.FromResult(_settings.FirstOrDefault(s => s.Id == id) ?? new BusinessSetting
        {
            CompanyName = "Test Company",
            CurrentFinancialYear = "2025-2026"
        });
    }

    public Task<IEnumerable<BusinessSetting>> GetAllSettingsAsync() => Task.FromResult(_settings);
    public Task AddSettingsAsync(BusinessSetting settings) => throw new NotSupportedException();
    public Task UpdateSettingsAsync(BusinessSetting settings) => throw new NotSupportedException();
    public Task DeleteSettingsAsync(int id) => throw new NotSupportedException();
}

internal sealed class CapturingAuthService : IAuthService
{
    private readonly User _user;

    public CapturingAuthService(User user)
    {
        _user = user;
    }

    public Task<User> LoginAsync(string username, string password) => throw new NotSupportedException();
    public Task<User> RegisterAsync(Grevity.Models.ViewModels.RegisterViewModel model) => Task.FromResult(_user);
    public Task<string> GenerateOtpAsync(string email) => throw new NotSupportedException();
    public Task<bool> VerifyOtpAsync(string email, string otp) => throw new NotSupportedException();
    public Task<bool> ResetPasswordAsync(Grevity.Models.ViewModels.ResetPasswordViewModel model) => throw new NotSupportedException();
}

internal sealed class NullEmailService : IEmailService
{
    public Task SendEmailAsync(string toEmail, string subject, string message) => Task.CompletedTask;
}

internal static class ControllerTestExtensions
{
    public static void SetUser(this Microsoft.AspNetCore.Mvc.Controller controller, params Claim[] claims)
    {
        var httpContext = controller.ControllerContext.HttpContext ?? new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = httpContext
        };
    }

    public static void SetTempData(this Microsoft.AspNetCore.Mvc.Controller controller)
    {
        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext ?? new DefaultHttpContext(),
            new TestTempDataProvider());
    }
}
