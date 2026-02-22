using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grevity.Services.Interfaces;
using Grevity.Models.Entities;
using Grevity.Models.ViewModels;

namespace Grevity.Controllers
{
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ISupplierService _supplierService;
        private readonly IProductService _productService;
        private readonly IBusinessSettingService _businessSettingService;
        private readonly ISubProductService _subProductService;
        private readonly ICompanyContext _companyContext;

        public PurchaseController(
            IInvoiceService invoiceService, 
            ISupplierService supplierService,
            IProductService productService,
            ISubProductService subProductService,
            IBusinessSettingService businessSettingService,
            ICompanyContext companyContext)
        {
            _invoiceService = invoiceService;
            _supplierService = supplierService;
            _productService = productService;
            _subProductService = subProductService;
            _businessSettingService = businessSettingService;
            _companyContext = companyContext;
        }

        public async Task<IActionResult> Index(string searchTerm, DocumentStage? stage)
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var purchases = invoices.Where(i => i.InvoiceType == "Purchase");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                purchases = purchases.Where(i => 
                    i.InvoiceNumber.ToLower().Contains(searchTerm) || 
                    (i.Supplier != null && i.Supplier.Name.ToLower().Contains(searchTerm))
                ).ToList();
            }

            if (stage.HasValue)
            {
                purchases = purchases.Where(i => i.Stage == stage.Value).ToList();
            }

            return View(purchases);
        }

        public async Task<IActionResult> Create(DocumentStage stage = DocumentStage.Order)
        {
            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            var viewModel = new InvoiceViewModel
            {
                Invoice = new Invoice { InvoiceType = "Purchase", Stage = stage, InvoiceDate = DateTime.Now },
                Products = (await _productService.GetAllProductsAsync()).Where(p => p.ItemType == "Purchase").ToList(),
                SubProducts = await _subProductService.GetAllSubProductsAsync()
            };
            ViewBag.Suppliers = await _supplierService.GetAllSuppliersAsync();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            invoice.InvoiceType = "Purchase";
            if (invoice.SupplierId == null) ModelState.AddModelError("Invoice.SupplierId", "Supplier is required");

            if (ModelState.IsValid)
            {
                await _invoiceService.CreateInvoiceAsync(invoice);
                TempData["Success"] = $"Purchase bill {invoice.InvoiceNumber} created successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Failed to create purchase record. Please check the entries.";

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            ViewBag.Suppliers = await _supplierService.GetAllSuppliersAsync();
            var viewModel = new InvoiceViewModel
            {
                Invoice = invoice,
                Products = (await _productService.GetAllProductsAsync()).Where(p => p.ItemType == "Purchase").ToList(),
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

            ViewBag.Suppliers = await _supplierService.GetAllSuppliersAsync();
            var viewModel = new InvoiceViewModel
            {
                Invoice = invoice,
                Products = (await _productService.GetAllProductsAsync()).Where(p => p.ItemType == "Purchase").ToList(),
                SubProducts = await _subProductService.GetAllSubProductsAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Invoice invoice)
        {
            if (id != invoice.Id) return NotFound();
            
            invoice.InvoiceType = "Purchase";
            if (invoice.SupplierId == null) ModelState.AddModelError("Invoice.SupplierId", "Supplier is required");

            if (ModelState.IsValid)
            {
                try 
                {
                    await _invoiceService.UpdateInvoiceAsync(invoice);
                    TempData["Success"] = $"Purchase bill {invoice.InvoiceNumber} updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                     TempData["Error"] = "Error updating purchase record: " + ex.Message;
                }
            }

            ViewBag.Suppliers = await _supplierService.GetAllSuppliersAsync();
            var viewModel = new InvoiceViewModel
            {
                Invoice = invoice,
                Products = (await _productService.GetAllProductsAsync()).Where(p => p.ItemType == "Purchase").ToList(),
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
                TempData["Success"] = "Purchase record deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting purchase record: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

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


    }
}
