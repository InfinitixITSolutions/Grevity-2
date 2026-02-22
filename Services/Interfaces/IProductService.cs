using System.Collections.Generic;
using System.Threading.Tasks;
using Grevity.Models.Entities;

namespace Grevity.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        
        // Composition methods
        Task<IEnumerable<ProductSubProductMapping>> GetProductCompositionAsync(int productId);
        Task UpdateProductCompositionAsync(int productId, IEnumerable<ProductSubProductMapping> mappings);
        Task<bool> ValidateStockForSaleAsync(int productId, decimal saleQuantity);
        Task<string> GetStockValidationMessageAsync(int productId, decimal saleQuantity);
    }
}
