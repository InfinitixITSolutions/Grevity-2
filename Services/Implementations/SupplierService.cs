using System.Collections.Generic;
using System.Threading.Tasks;
using Grevity.Models.Entities;
using Grevity.Repositories.Interfaces;
using Grevity.Services.Interfaces;

namespace Grevity.Services.Implementations
{
    public class SupplierService : ISupplierService
    {
        private readonly IRepository<Supplier> _repository;

        public SupplierService(IRepository<Supplier> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Supplier>> GetAllSuppliersAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Supplier> GetSupplierByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddSupplierAsync(Supplier supplier)
        {
            await _repository.AddAsync(supplier);
        }

        public async Task UpdateSupplierAsync(Supplier supplier)
        {
            await _repository.UpdateAsync(supplier);
        }

        public async Task DeleteSupplierAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
