using System.Collections.Generic;
using System.Threading.Tasks;
using Grevity.Models.Entities;

namespace Grevity.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<Invoice> GetInvoiceByIdAsync(int id);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
        Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
        Task<string> GenerateInvoiceNumberAsync();
        Task AddPaymentTransactionAsync(int invoiceId, PaymentTransaction transaction);
        Task<IEnumerable<PaymentTransaction>> GetPaymentTransactionsAsync(int invoiceId);
        Task DeleteInvoiceAsync(int id);
        Task<Invoice> UpdateStageAsync(int invoiceId, DocumentStage newStage);
        Task UpdatePaymentAsync(int invoiceId, decimal newPaidAmount); // Keep for compatibility if needed, but better to remove or deprecate
    }
}
