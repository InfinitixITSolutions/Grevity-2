using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Grevity.Services.Interfaces;
using Grevity.Models.ViewModels;

using Grevity.Models.Entities;
using Grevity.Repositories.Interfaces;

namespace Grevity.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly ICompanyContext _companyContext;
        private readonly IRepository<UserCompany> _userCompanyRepository;
        private readonly IRepository<BusinessSetting> _companyRepository;

        public AccountController(
            IAuthService authService, 
            IEmailService emailService, 
            ICompanyContext companyContext,
            IRepository<UserCompany> userCompanyRepository,
            IRepository<BusinessSetting> companyRepository)
        {
            _authService = authService;
            _emailService = emailService;
            _companyContext = companyContext;
            _userCompanyRepository = userCompanyRepository;
            _companyRepository = companyRepository;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _authService.LoginAsync(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid Username or Password");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Load Default Company
            var userCompany = (await _userCompanyRepository.GetAllAsync())
                .FirstOrDefault(uc => uc.UserId == user.Id && uc.IsDefault);
            
            if (userCompany != null)
            {
                await _companyContext.SetCompanyAsync(userCompany.BusinessSettingId);
            }
            else
            {
                // Fallback: Pick any company
                var anyCompany = (await _userCompanyRepository.GetAllAsync())
                    .FirstOrDefault(uc => uc.UserId == user.Id);
                
                if (anyCompany != null)
                {
                    await _companyContext.SetCompanyAsync(anyCompany.BusinessSettingId);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _authService.RegisterAsync(model);
            if (user == null)
            {
                ModelState.AddModelError("", "Username or Email already exists.");
                return View(model);
            }

            // Create Default Company for new user
            var defaultCompany = new BusinessSetting
            {
                CompanyName = $"{user.Username}'s Company",
                CurrentFinancialYear = "2024-2025"
            };
            await _companyRepository.AddAsync(defaultCompany);

            // Link User to Company
            var userCompany = new UserCompany
            {
                UserId = user.Id,
                BusinessSettingId = defaultCompany.Id,
                IsDefault = true
            };
            await _userCompanyRepository.AddAsync(userCompany);

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var otp = await _authService.GenerateOtpAsync(model.Email);
            if (otp != null)
            {
                await _emailService.SendEmailAsync(model.Email, "Reset Password OTP", $"Your OTP is: {otp}");
                return RedirectToAction("VerifyOtp", new { email = model.Email });
            }

            ModelState.AddModelError("", "Email not found.");
            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var isValid = await _authService.VerifyOtpAsync(model.Email, model.Otp);
            if (isValid)
            {
                return RedirectToAction("ResetPassword", new { email = model.Email, otp = model.Otp });
            }

            ModelState.AddModelError("", "Invalid or Expired OTP.");
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string otp)
        {
            return View(new ResetPasswordViewModel { Email = email, Otp = otp });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _authService.ResetPasswordAsync(model);
            if (result)
            {
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Password Reset Failed. Try again.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SwitchCompany(int companyId)
        {
             // Verify user has access to this company
             var userId = int.Parse(User.FindFirst("UserId").Value);
             var hasAccess = (await _userCompanyRepository.GetAllAsync())
                 .Any(uc => uc.UserId == userId && uc.BusinessSettingId == companyId);
             
             if (hasAccess)
             {
                 await _companyContext.SetCompanyAsync(companyId);
             }
             
             return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
