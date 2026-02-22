using System.Threading.Tasks;
using Grevity.Models.Entities;
using Grevity.Repositories.Interfaces;
using Grevity.Services.Interfaces;

namespace Grevity.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;

        public AuthService(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            // In a real app, hash the password before comparing
            // Using Filter for GetAsync
            var user = await _userRepository.GetAsync(u => u.Username == username && u.PasswordHash == password);
            return user;
        }

        public async Task<User> RegisterAsync(Grevity.Models.ViewModels.RegisterViewModel model)
        {
            var existingUser = await _userRepository.GetAsync(u => u.Email == model.Email || u.Username == model.Username);
            if (existingUser != null)
            {
                return null; // Or throw exception
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = model.Password, // Implement hashing here later
                Role = "User"
            };

            await _userRepository.AddAsync(user);
            // Save changes is usually handled by repository or unit of work
            return user;
        }

        public async Task<string> GenerateOtpAsync(string email)
        {
            var user = await _userRepository.GetAsync(u => u.Email == email);
            if (user == null) return null;

            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiryTime = DateTime.Now.AddMinutes(15);

            await _userRepository.UpdateAsync(user);
            return otp;
        }

        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var user = await _userRepository.GetAsync(u => u.Email == email && u.OtpCode == otp);
            if (user == null) return false;

            if (user.OtpExpiryTime < DateTime.Now)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> ResetPasswordAsync(Grevity.Models.ViewModels.ResetPasswordViewModel model)
        {
             var user = await _userRepository.GetAsync(u => u.Email == model.Email);
             if (user == null) return false;
             
             // Double check OTP just in case, though VerifyOtp should be called before this
             if (user.OtpCode != model.Otp || user.OtpExpiryTime < DateTime.Now)
             {
                 return false;
             }

             user.PasswordHash = model.NewPassword; // Hash this
             user.OtpCode = null;
             user.OtpExpiryTime = null;

             await _userRepository.UpdateAsync(user);
             return true;
        }
    }
}
