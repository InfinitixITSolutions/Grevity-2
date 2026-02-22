using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grevity.Data;
using Grevity.Models.Entities;
using Grevity.Repositories.Interfaces;
using Grevity.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Grevity.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _repository;
        private readonly IRepository<ProductSubProductMapping> _mappingRepository;
        private readonly AppDbContext _context;

        public ProductService(
            IRepository<Product> repository, 
            IRepository<ProductSubProductMapping> mappingRepository,
            AppDbContext context)
        {
            _repository = repository;
            _mappingRepository = mappingRepository;
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            // Include SubProductMappings and their SubProducts
            return await _context.Products
                .Include(p => p.SubProductMappings)
                .ThenInclude(m => m.SubProduct)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddProductAsync(Product product)
        {
            await _repository.AddAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _repository.UpdateAsync(product);
        }

        public async Task DeleteProductAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<ProductSubProductMapping>> GetProductCompositionAsync(int productId)
        {
            return await _context.ProductSubProductMappings
                .Include(m => m.SubProduct)
                .Where(m => m.ProductId == productId)
                .ToListAsync();
        }

        public async Task UpdateProductCompositionAsync(int productId, IEnumerable<ProductSubProductMapping> mappings)
        {
            // Remove existing mappings
            var existingMappings = await _context.ProductSubProductMappings
                .Where(m => m.ProductId == productId)
                .ToListAsync();
            
            _context.ProductSubProductMappings.RemoveRange(existingMappings);

            // Add new mappings
            foreach (var mapping in mappings)
            {
                mapping.ProductId = productId;
                await _mappingRepository.AddAsync(mapping);
            }
        }

        public async Task<bool> ValidateStockForSaleAsync(int productId, decimal saleQuantity)
        {
            var product = await GetProductByIdAsync(productId);
            if (product == null) return false;

            // If it has no sub-products, it's a regular product (stock check logic for regular products can be added if needed)
            if (product.SubProductMappings == null || !product.SubProductMappings.Any())
                return true;

            foreach (var mapping in product.SubProductMappings)
            {
                decimal totalRequired = mapping.RequiredQuantity * saleQuantity;
                if (mapping.SubProduct.CurrentStock < totalRequired)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<string> GetStockValidationMessageAsync(int productId, decimal saleQuantity)
        {
            var product = await GetProductByIdAsync(productId);
            if (product == null) return "Product not found.";

            if (product.SubProductMappings == null || !product.SubProductMappings.Any())
                return string.Empty;

            var insufficientItems = new List<string>();

            foreach (var mapping in product.SubProductMappings)
            {
                decimal totalRequired = mapping.RequiredQuantity * saleQuantity;
                if (mapping.SubProduct.CurrentStock < totalRequired)
                {
                    insufficientItems.Add($"{mapping.SubProduct.Name} (Required: {totalRequired} {mapping.SubProduct.Unit}, Available: {mapping.SubProduct.CurrentStock} {mapping.SubProduct.Unit})");
                }
            }

            if (insufficientItems.Any())
            {
                return "Insufficient stock for: " + string.Join(", ", insufficientItems);
            }

            return string.Empty;
        }
    }
}
