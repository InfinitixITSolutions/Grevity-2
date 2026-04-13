using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grevity.Services.Interfaces;
using Grevity.Models.Entities;

namespace Grevity.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;
        private readonly IBusinessSettingService _businessSettingService;
        private readonly ICompanyContext _companyContext;

        public SupplierController(
            ISupplierService supplierService,
            IBusinessSettingService businessSettingService,
            ICompanyContext companyContext)
        {
            _supplierService = supplierService;
            _businessSettingService = businessSettingService;
            _companyContext = companyContext;
        }

        public async Task<IActionResult> Index(string? searchTerm)
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                suppliers = suppliers.Where(s => 
                    (s.Name != null && s.Name.ToLower().Contains(searchTerm)) ||
                    (s.Mobile != null && s.Mobile.Contains(searchTerm)) ||
                    (s.Email != null && s.Email.ToLower().Contains(searchTerm))
                ).ToList();
            }

            ViewBag.SearchTerm = searchTerm;
            return View(suppliers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null) return NotFound();

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View(supplier);
        }

        public async Task<IActionResult> Create()
        {
            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                await _supplierService.AddSupplierAsync(supplier);
                TempData["Success"] = "Supplier created successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Please correct the errors in the form.";

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View(supplier);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null) return NotFound();

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _supplierService.UpdateSupplierAsync(supplier);
                TempData["Success"] = "Supplier updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Please correct the errors in the form.";

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try 
            {
                await _supplierService.DeleteSupplierAsync(id);
                TempData["Success"] = "Supplier deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting supplier: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
