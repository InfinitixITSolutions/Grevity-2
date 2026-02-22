using System.Threading.Tasks;
using Grevity.Models.Entities;

namespace Grevity.Services.Interfaces
{
    public interface IBusinessSettingService
    {
        Task<BusinessSetting> GetSettingsAsync(int? id = null, bool asNoTracking = false);
        Task<IEnumerable<BusinessSetting>> GetAllSettingsAsync();
        Task AddSettingsAsync(BusinessSetting settings);
        Task UpdateSettingsAsync(BusinessSetting settings);
        Task DeleteSettingsAsync(int id);
    }
}
