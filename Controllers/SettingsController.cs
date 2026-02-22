using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Grevity.Services.Interfaces;
using Grevity.Models.Entities;
using Grevity.Repositories.Interfaces;

namespace Grevity.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly IBusinessSettingService _settingService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDataProtector _protector;
        private readonly IRepository<UserCompany> _userCompanyRepository;
        private readonly ICompanyContext _companyContext;

        public SettingsController(
            IBusinessSettingService settingService, 
            IWebHostEnvironment webHostEnvironment, 
            IDataProtectionProvider provider,
            IRepository<UserCompany> userCompanyRepository,
            ICompanyContext companyContext)
        {
            _settingService = settingService;
            _webHostEnvironment = webHostEnvironment;
            _protector = provider.CreateProtector("Grevity.Settings.EmailPassword");
            _userCompanyRepository = userCompanyRepository;
            _companyContext = companyContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? id)
        {
            BusinessSetting settings;

            // If explicit "Create New" (id == 0), initialize blank
            if (id.HasValue && id.Value == 0)
            {
                settings = new BusinessSetting();
            }
            else
            {
                // If no ID provided, use the current active company from session
                if (!id.HasValue)
                {
                    var activeId = _companyContext.CurrentCompanyId;
                    if (activeId.HasValue)
                    {
                        id = activeId;
                    }
                }

                settings = await _settingService.GetSettingsAsync(id);
            }
            
            // If it's a new company request (id=0), settings will be null here
            if (settings == null && id.HasValue && id.Value == 0)
            {
                settings = new BusinessSetting();
            }

            if (settings != null && !string.IsNullOrEmpty(settings.EmailPassword))
            {
                try
                {
                    settings.EmailPassword = _protector.Unprotect(settings.EmailPassword);
                }
                catch
                {
                    settings.EmailPassword = string.Empty;
                }
            }

            // Show all companies ASSIGNED to the current user
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var companyIds = (await _userCompanyRepository.GetAllAsync())
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.BusinessSettingId)
                    .ToList();

                var userCompanies = (await _settingService.GetAllSettingsAsync())
                    .Where(c => companyIds.Contains(c.Id))
                    .ToList();
                
                ViewBag.AllCompanies = userCompanies;
            }
            else
            {
                ViewBag.AllCompanies = new List<BusinessSetting>();
            }
            
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Index(BusinessSetting model, IFormFile? logo)
        {
            // Remove validation for CompanyName if needed, but it's required in model. 
            // If creating new, Id is 0.
            
            // Remove EmailPassword from validation errors because we might be manipulating it
            ModelState.Remove(nameof(model.EmailPassword));

            if (ModelState.IsValid)
            {
                // Fetch existing as no-tracking to avoid conflict during update
                BusinessSetting? existing = null;
                if (model.Id > 0)
                {
                    existing = await _settingService.GetSettingsAsync(model.Id, asNoTracking: true);
                }

                if (logo != null && logo.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/logos");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + logo.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await logo.CopyToAsync(fileStream);
                    }

                    model.LogoPath = "/images/logos/" + uniqueFileName;
                }
                else
                {
                    // If no new logo uploaded, keep existing one
                    if (existing != null && (string.IsNullOrEmpty(model.LogoPath) || model.LogoPath == existing.LogoPath))
                    {
                        model.LogoPath = existing.LogoPath;
                    }
                }

                // Encrypt Password
                if (!string.IsNullOrEmpty(model.EmailPassword))
                {
                    model.EmailPassword = _protector.Protect(model.EmailPassword);
                }
                else
                {
                     // If empty, user might intend to keep existing password OR clear it.
                     // Standard behavior: if empty and Updating, keep existing.
                     if (existing != null && !string.IsNullOrEmpty(existing.EmailPassword))
                     {
                         model.EmailPassword = existing.EmailPassword;
                     }
                }
                
                if (model.Id == 0)
                {
                    await _settingService.AddSettingsAsync(model);
                    
                    // Link to User
                    var userIdStr = User.FindFirst("UserId")?.Value;
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        var userCompany = new UserCompany
                        {
                            UserId = userId,
                            BusinessSettingId = model.Id,
                            IsDefault = false
                        };
                        await _userCompanyRepository.AddAsync(userCompany);
                    }
                }
                else
                {
                    await _settingService.UpdateSettingsAsync(model);
                }

                TempData["Success"] = "Settings saved successfully!";
                // If we just created or updated, we want to stay on that company's settings
                return RedirectToAction("Index", new { id = model.Id });
            }

            // If we are here, ModelState is invalid. Reload the company list for the user.
            var uIdStr = User.FindFirst("UserId")?.Value;
            if (int.TryParse(uIdStr, out int uId))
            {
                var cIds = (await _userCompanyRepository.GetAllAsync())
                    .Where(uc => uc.UserId == uId)
                    .Select(uc => uc.BusinessSettingId)
                    .ToList();

                var userCompList = (await _settingService.GetAllSettingsAsync())
                    .Where(c => cIds.Contains(c.Id))
                    .ToList();
                ViewBag.AllCompanies = userCompList;
            }
            else
            {
                ViewBag.AllCompanies = new List<BusinessSetting>();
            }

            return View(model);
        }
        public async Task<IActionResult> List()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var cIds = (await _userCompanyRepository.GetAllAsync())
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.BusinessSettingId)
                    .ToList();

                var userCompanies = (await _settingService.GetAllSettingsAsync())
                    .Where(c => cIds.Contains(c.Id))
                    .ToList();
                
                return View(userCompanies);
            }
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _settingService.DeleteSettingsAsync(id);
                TempData["Success"] = "Company and all associated data deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting company: " + ex.Message;
            }
            return RedirectToAction(nameof(List));
        }
    }
}
