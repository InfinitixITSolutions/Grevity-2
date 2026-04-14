using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grevity.Models.Entities;
using Grevity.Services.Interfaces;
using Grevity.Models.ViewModels;

namespace Grevity.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IInvoiceService _invoiceService;

        public HomeController(
            ICustomerService customerService,
            IProductService productService,
            IInvoiceService invoiceService)
        {
            _customerService = customerService;
            _productService = productService;
            _invoiceService = invoiceService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            var products = await _productService.GetAllProductsAsync();
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var salesInvoices = invoices
                .Where(i => i.InvoiceType == "Sale" && i.Stage == DocumentStage.Invoice)
                .ToList();

            var model = new DashboardViewModel
            {
                TotalCustomers = customers.Count(),
                TotalProducts = products.Count(),
                TotalInvoices = salesInvoices.Count(),
                TotalSales = salesInvoices.Sum(i => i.GrandTotal)
                // Outstanding calculation would require logic
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }
        
        // Error handling
    }
}
