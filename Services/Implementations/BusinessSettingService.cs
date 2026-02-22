using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Grevity.Models.Entities;
using Grevity.Repositories.Interfaces;
using Grevity.Services.Interfaces;

namespace Grevity.Services.Implementations
{
    public class BusinessSettingService : IBusinessSettingService
    {
        private readonly IRepository<BusinessSetting> _repository;
        private readonly ICompanyContext _companyContext;
        private readonly Grevity.Data.AppDbContext _context;

        public BusinessSettingService(
            IRepository<BusinessSetting> repository, 
            ICompanyContext companyContext,
            Grevity.Data.AppDbContext context)
        {
            _repository = repository;
            _companyContext = companyContext;
            _context = context;
        }

        public async Task<BusinessSetting> GetSettingsAsync(int? id = null, bool asNoTracking = false)
        {
            if (id.HasValue && id.Value > 0)
            {
                return asNoTracking 
                    ? await _repository.GetByIdNoTrackingAsync(id.Value) 
                    : await _repository.GetByIdAsync(id.Value);
            }
            
            // Use current active company ID if available
            var activeId = _companyContext.CurrentCompanyId;
            if (activeId.HasValue)
            {
                return asNoTracking 
                    ? await _repository.GetByIdNoTrackingAsync(activeId.Value) 
                    : await _repository.GetByIdAsync(activeId.Value);
            }

            // Fallback to first record (should ideally not happen with multi-company login)
            var all = await _repository.GetAllAsync();
            var settings = all.FirstOrDefault();
            return settings;
        }

        public async Task<IEnumerable<BusinessSetting>> GetAllSettingsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task AddSettingsAsync(BusinessSetting settings)
        {
            await _repository.AddAsync(settings);
        }

        public async Task UpdateSettingsAsync(BusinessSetting settings)
        {
            await _repository.UpdateAsync(settings);
        }

        public async Task DeleteSettingsAsync(int id)
        {
            // 1. Delete all transactional and master data for this company
            _context.InvoiceItems.RemoveRange(_context.InvoiceItems.IgnoreQueryFilters().Where(i => i.CompanyId == id));
            _context.Invoices.RemoveRange(_context.Invoices.IgnoreQueryFilters().Where(i => i.CompanyId == id));
            _context.PaymentTransactions.RemoveRange(_context.PaymentTransactions.IgnoreQueryFilters().Where(i => i.CompanyId == id));
            _context.Products.RemoveRange(_context.Products.IgnoreQueryFilters().Where(i => i.CompanyId == id));
            _context.Customers.RemoveRange(_context.Customers.IgnoreQueryFilters().Where(i => i.CompanyId == id));
            _context.Suppliers.RemoveRange(_context.Suppliers.IgnoreQueryFilters().Where(i => i.CompanyId == id));
            _context.AuditLogs.RemoveRange(_context.AuditLogs.Where(i => i.CompanyId == id));
            
            // 2. Delete user-company mappings
            _context.UserCompanies.RemoveRange(_context.UserCompanies.Where(uc => uc.BusinessSettingId == id));

            // 3. Delete the company settings itself
            var settings = await _repository.GetByIdAsync(id);
            if (settings != null)
            {
                await _repository.DeleteAsync(settings);
            }

            await _context.SaveChangesAsync();
        }
    }
}
