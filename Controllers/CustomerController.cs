using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grevity.Services.Interfaces;
using Grevity.Models.Entities;

namespace Grevity.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IBusinessSettingService _businessSettingService;
        private readonly ICompanyContext _companyContext;

        public CustomerController(
            ICustomerService customerService,
            IBusinessSettingService businessSettingService,
            ICompanyContext companyContext)
        {
            _customerService = customerService;
            _businessSettingService = businessSettingService;
            _companyContext = companyContext;
        }

        public async Task<IActionResult> Index(string searchTerm)
        {
            var customers = await _customerService.GetAllCustomersAsync();
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                customers = customers.Where(c => 
                    (c.Name != null && c.Name.ToLower().Contains(searchTerm)) || 
                    (c.Mobile != null && c.Mobile.Contains(searchTerm)) || 
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm))
                ).ToList();
            }

            return View(customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View(customer);
        }

        public async Task<IActionResult> Create()
        {
            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerService.AddCustomerAsync(customer);
                TempData["Success"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Please correct the errors in the form.";

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View(customer);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _customerService.UpdateCustomerAsync(customer);
                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Please correct the errors in the form.";

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;

            return View(customer);
        }

        public async Task<IActionResult> Delete(int id)
        {
            try 
            {
                await _customerService.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting customer: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid Excel file.";
                return RedirectToAction(nameof(Import));
            }

            try 
            {
                using (var stream = new System.IO.MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                        int count = 0;
                        foreach (var row in rows)
                        {
                            var customer = new Customer
                            {
                                Name = row.Cell(1).GetValue<string>(),
                                Mobile = row.Cell(2).GetValue<string>(),
                                Email = row.Cell(3).GetValue<string>(),
                                Address = row.Cell(4).GetValue<string>(),
                                GSTIN = row.Cell(5).GetValue<string>(),
                                OpeningBalance = row.Cell(6).GetValue<decimal>()
                            };
                            await _customerService.AddCustomerAsync(customer);
                            count++;
                        }
                        TempData["Success"] = $"{count} customers imported successfully!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error importing customers: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
