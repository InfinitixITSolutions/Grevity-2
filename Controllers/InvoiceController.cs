using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grevity.Services.Interfaces;
using Grevity.Models.Entities;
using Grevity.Models.ViewModels;

namespace Grevity.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IBusinessSettingService _businessSettingService;
        private readonly ISubProductService _subProductService;
        private readonly ICompanyContext _companyContext;

        public InvoiceController(
            IInvoiceService invoiceService, 
            ICustomerService customerService,
            IProductService productService,
            ISubProductService subProductService,
            IBusinessSettingService businessSettingService,
            ICompanyContext companyContext)
        {
            _invoiceService = invoiceService;
            _customerService = customerService;
            _productService = productService;
            _subProductService = subProductService;
            _businessSettingService = businessSettingService;
            _companyContext = companyContext;
        }

        public async Task<IActionResult> Index(string searchTerm, DocumentStage? stage)
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            invoices = invoices.Where(i => i.InvoiceType == "Sale");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                invoices = invoices.Where(i => 
                    i.InvoiceNumber.ToLower().Contains(searchTerm) || 
                    (i.Customer != null && i.Customer.Name.ToLower().Contains(searchTerm))
                ).ToList();
            }

            if (stage.HasValue)
            {
                invoices = invoices.Where(i => i.Stage == stage.Value).ToList();
            }

            return View(invoices);
        }

        public async Task<IActionResult> Create(DocumentStage stage = DocumentStage.Invoice)
        {
            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            var viewModel = new InvoiceViewModel
            {
                Invoice = new Invoice { Stage = stage, InvoiceDate = DateTime.Now },
                Customers = await _customerService.GetAllCustomersAsync(),
                Products = (await _productService.GetAllProductsAsync()).Where(p => p.ItemType == "Sales").ToList(),
                SubProducts = await _subProductService.GetAllSubProductsAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            invoice.InvoiceType = "Sale";
            if (invoice.CustomerId == null) ModelState.AddModelError("Invoice.CustomerId", "Customer is required");
            if (invoice.InvoiceItems == null || !invoice.InvoiceItems.Any()) ModelState.AddModelError("", "At least one item is required");
            
            // Remove validation for InvoiceNumber as it is auto-generated
            ModelState.Remove("InvoiceNumber");
            ModelState.Remove("Invoice.InvoiceNumber");
            ModelState.Remove("invoice.InvoiceNumber");

            if (ModelState.IsValid)
            {
                await _invoiceService.CreateInvoiceAsync(invoice);
                TempData["Success"] = $"Invoice {invoice.InvoiceNumber} created successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Failed to create invoice. Please check the entries.";

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            // Reload data for dropdowns if failure
            var viewModel = new InvoiceViewModel
            {
                Invoice = invoice,
                Customers = await _customerService.GetAllCustomersAsync(),
                Products = (await _productService.GetAllProductsAsync()).Where(p => p.ItemType == "Sales").ToList(),
                SubProducts = await _subProductService.GetAllSubProductsAsync()
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            var viewModel = new InvoiceViewModel
            {
                Invoice = invoice,
                Customers = await _customerService.GetAllCustomersAsync(),
                Products = (await _productService.GetAllProductsAsync()).Where(p => p.ItemType == "Sales").ToList(),
                SubProducts = await _subProductService.GetAllSubProductsAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Invoice invoice)
        {
            if (invoice.CustomerId == null) ModelState.AddModelError("Invoice.CustomerId", "Customer is required");
            if (invoice.InvoiceItems == null || !invoice.InvoiceItems.Any()) ModelState.AddModelError("", "At least one item is required");
            
            // Remove validation for InvoiceNumber/Related entities
            ModelState.Remove("InvoiceNumber");
            ModelState.Remove("Invoice.InvoiceNumber");
            ModelState.Remove("invoice.InvoiceNumber");

            if (ModelState.IsValid)
            {
                try
                {
                    await _invoiceService.UpdateInvoiceAsync(invoice);
                    TempData["Success"] = $"Invoice {invoice.InvoiceNumber} updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating invoice: " + ex.Message);
                }
            }
            TempData["Error"] = "Failed to update invoice. Please check the entries.";

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            var viewModel = new InvoiceViewModel
            {
                Invoice = invoice,
                Customers = await _customerService.GetAllCustomersAsync(),
                Products = (await _productService.GetAllProductsAsync()).Where(p => p.ItemType == "Sales").ToList(),
                SubProducts = await _subProductService.GetAllSubProductsAsync()
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;
            ViewBag.BusinessSetting = settings;

            return View(invoice);
        }

        public async Task<IActionResult> Delete(int id)
        {
            try 
            {
                await _invoiceService.DeleteInvoiceAsync(id);
                TempData["Success"] = "Invoice deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting invoice: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ConvertStage(int id, DocumentStage newStage)
        {
            try
            {
                await _invoiceService.UpdateStageAsync(id, newStage);
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }
        

        
        // API Endpoint for JS to get product details
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Json(product);
        }
    }
}
