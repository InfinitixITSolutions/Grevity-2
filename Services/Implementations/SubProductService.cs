using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Grevity.Data;
using Grevity.Models.Entities;
using Grevity.Services.Interfaces;

namespace Grevity.Services.Implementations
{
    public class SubProductService : ISubProductService
    {
        private readonly AppDbContext _context;

        public SubProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubProduct>> GetAllSubProductsAsync()
        {
            return await _context.SubProducts
                .Where(sp => sp.IsActive)
                .OrderBy(sp => sp.Name)
                .ToListAsync();
        }

        public async Task<SubProduct> GetSubProductByIdAsync(int id)
        {
            return await _context.SubProducts.FindAsync(id);
        }

        public async Task AddSubProductAsync(SubProduct subProduct)
        {
            _context.SubProducts.Add(subProduct);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSubProductAsync(SubProduct subProduct)
        {
            subProduct.UpdatedAt = DateTime.Now;
            _context.SubProducts.Update(subProduct);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSubProductAsync(int id)
        {
            var subProduct = await _context.SubProducts.FindAsync(id);
            if (subProduct != null)
            {
                // Check if sub product is used in any main product mappings
                var isUsed = await _context.ProductSubProductMappings
                    .AnyAsync(m => m.SubProductId == id);

                if (isUsed)
                {
                    throw new InvalidOperationException(
                        "Cannot delete Sub Product as it is used in one or more Main Products. " +
                        "Remove it from all Main Products first.");
                }

                subProduct.IsActive = false;
                subProduct.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task IncreaseStockAsync(int subProductId, decimal quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.");
            }

            var subProduct = await _context.SubProducts.FindAsync(subProductId);
            if (subProduct == null)
            {
                throw new InvalidOperationException($"Sub Product with ID {subProductId} not found.");
            }

            subProduct.CurrentStock += quantity;
            subProduct.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task DecreaseStockAsync(int subProductId, decimal quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.");
            }

            var subProduct = await _context.SubProducts.FindAsync(subProductId);
            if (subProduct == null)
            {
                throw new InvalidOperationException($"Sub Product with ID {subProductId} not found.");
            }

            if (subProduct.CurrentStock < quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for '{subProduct.Name}'. " +
                    $"Available: {subProduct.CurrentStock} {subProduct.Unit}, Required: {quantity} {subProduct.Unit}");
            }

            subProduct.CurrentStock -= quantity;
            subProduct.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ValidateStockAvailabilityAsync(int subProductId, decimal requiredQuantity)
        {
            var subProduct = await _context.SubProducts.FindAsync(subProductId);
            if (subProduct == null)
            {
                return false;
            }

            return subProduct.CurrentStock >= requiredQuantity;
        }
    }
}
