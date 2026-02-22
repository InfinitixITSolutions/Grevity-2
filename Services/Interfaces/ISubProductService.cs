using System.Collections.Generic;
using System.Threading.Tasks;
using Grevity.Models.Entities;

namespace Grevity.Services.Interfaces
{
    public interface ISubProductService
    {
        Task<IEnumerable<SubProduct>> GetAllSubProductsAsync();
        Task<SubProduct> GetSubProductByIdAsync(int id);
        Task AddSubProductAsync(SubProduct subProduct);
        Task UpdateSubProductAsync(SubProduct subProduct);
        Task DeleteSubProductAsync(int id);
        
        // Stock management operations
        Task IncreaseStockAsync(int subProductId, decimal quantity);
        Task DecreaseStockAsync(int subProductId, decimal quantity);
        Task<bool> ValidateStockAvailabilityAsync(int subProductId, decimal requiredQuantity);
    }
}
