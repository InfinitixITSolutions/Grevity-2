using System.Threading.Tasks;
using Grevity.Models.Entities;

namespace Grevity.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User> LoginAsync(string username, string password);
        Task<User> RegisterAsync(Grevity.Models.ViewModels.RegisterViewModel model);
        Task<string> GenerateOtpAsync(string email);
        Task<bool> VerifyOtpAsync(string email, string otp);
        Task<bool> ResetPasswordAsync(Grevity.Models.ViewModels.ResetPasswordViewModel model);
    }
}
