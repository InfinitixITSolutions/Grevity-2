using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Grevity.Data;
using Grevity.Models.Entities;
using Grevity.Repositories.Interfaces;
using Grevity.Services.Interfaces;

namespace Grevity.Services.Implementations
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IRepository<Invoice> _invoiceRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly AppDbContext _context;

        public InvoiceService(
            IRepository<Invoice> invoiceRepository,
            IRepository<Product> productRepository,
            IRepository<Customer> customerRepository,
            IRepository<Supplier> supplierRepository,
            AppDbContext context)
        {
            _invoiceRepository = invoiceRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _supplierRepository = supplierRepository;
            _context = context;
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<Invoice> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .Include(i => i.InvoiceItems)
                .ThenInclude(it => it.Product)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var now = DateTime.Now;
            int financialYear = now.Month >= 4 ? now.Year : now.Year - 1;
            string suffix = (financialYear % 100).ToString("D2");

            var lastInvoice = await _context.Invoices
                .Where(i => i.InvoiceNumber.EndsWith("-" + suffix))
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            if (lastInvoice == null)
            {
                return $"1-{suffix}";
            }

            // Extract number from {sequence}-{suffix}
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length > 0 && int.TryParse(parts[0], out int number))
            {
                return $"{number + 1}-{suffix}";
            }
            
            return $"{DateTime.Now.Ticks}-{suffix}"; // Fallback
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();
                    if (invoice.InvoiceDate == default)
                    {
                        invoice.InvoiceDate = DateTime.Now;
                    }
                    
                    decimal totalAmount = 0;
                    decimal totalTax = 0;

                    foreach (var item in invoice.InvoiceItems)
                    {
                        if (item.ProductId == null && item.SubProductId == null) throw new Exception("Product or Sub-Product not found");

                        if (item.ProductId.HasValue)
                        {
                            var product = await _productRepository.GetByIdAsync(item.ProductId.Value);
                            if (product == null) throw new Exception($"Product not found: {item.ProductId}");
                            item.ProductName = product.Name;
                            item.HSN = product.HSN;
                        }
                        else if (item.SubProductId.HasValue)
                        {
                            var subProduct = await _context.SubProducts.FindAsync(item.SubProductId.Value);
                            if (subProduct == null) throw new Exception($"Sub-Product not found: {item.SubProductId}");
                            item.ProductName = subProduct.Name;
                            item.HSN = ""; // Sub products might not have HSN in current model, or we can add it later
                        }

                        // Stock Logic - Only for Invoices/Bills
                        if (invoice.Stage == DocumentStage.Invoice)
                        {
                            await UpdateProductAndSubProductStockAsync(item, invoice.InvoiceType, false);
                        }


                        decimal taxableValue = item.Quantity * item.UnitPrice;
                        decimal taxValue = (taxableValue * (item.GSTPercentage.HasValue ? item.GSTPercentage.Value : 0)) / 100;
                        item.TaxAmount = taxValue;
                        item.TotalAmount = taxableValue + taxValue;

                        totalAmount += taxableValue;
                        totalTax += taxValue;
                    }

                    invoice.TaxAmount = totalTax;
                    decimal subTotal = totalAmount + totalTax;
                    invoice.GrandTotal = subTotal - invoice.DiscountAmount;
                    decimal roundedTotal = Math.Round(invoice.GrandTotal);
                    invoice.RoundOff = roundedTotal - invoice.GrandTotal;
                    invoice.GrandTotal = roundedTotal;

                    // Validate PaidAmount
                    if (invoice.PaidAmount < 0)
                        throw new Exception("Paid amount cannot be negative");
                    if (invoice.PaidAmount > invoice.GrandTotal)
                        throw new Exception("Paid amount cannot exceed grand total");

                    // Auto-calculate payment status
                    invoice.Status = invoice.PaymentStatus;

                    // Save Invoice
                    await _context.Invoices.AddAsync(invoice);
                    await _context.SaveChangesAsync();

                    // Update Balance Logic - Only for Invoices/Bills
                    if (invoice.Stage == DocumentStage.Invoice)
                    {
                        if (invoice.CustomerId.HasValue)
                        {
                            var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId.Value);
                            if (customer != null)
                            {
                                customer.CurrentBalance += (invoice.GrandTotal - invoice.PaidAmount);
                                await _customerRepository.UpdateAsync(customer);
                            }
                        }
                        else if (invoice.SupplierId.HasValue)
                        {
                            var supplier = await _supplierRepository.GetByIdAsync(invoice.SupplierId.Value);
                            if (supplier != null)
                            {
                                supplier.CurrentBalance += (invoice.GrandTotal - invoice.PaidAmount);
                                await _supplierRepository.UpdateAsync(supplier);
                            }
                        }
                    }

                    await transaction.CommitAsync();
                    return invoice;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var existingInvoice = await _context.Invoices
                        .Include(i => i.InvoiceItems)
                        .FirstOrDefaultAsync(i => i.Id == invoice.Id);

                    if (existingInvoice == null) throw new Exception("Invoice not found");

                    // 1. Revert previous stock/balance if it was already in Invoice stage
                    if (existingInvoice.Stage == DocumentStage.Invoice)
                    {
                        foreach (var item in existingInvoice.InvoiceItems)
                        {
                            await UpdateProductAndSubProductStockAsync(item, existingInvoice.InvoiceType, true);
                        }

                        if (existingInvoice.CustomerId.HasValue)
                        {
                            var customer = await _customerRepository.GetByIdAsync(existingInvoice.CustomerId.Value);
                            if (customer != null) customer.CurrentBalance -= (existingInvoice.GrandTotal - existingInvoice.PaidAmount);
                        }
                        else if (existingInvoice.SupplierId.HasValue)
                        {
                            var supplier = await _supplierRepository.GetByIdAsync(existingInvoice.SupplierId.Value);
                            if (supplier != null) supplier.CurrentBalance -= (existingInvoice.GrandTotal - existingInvoice.PaidAmount);
                        }
                    }

                    // 2. Update basic properties
                    existingInvoice.CustomerId = invoice.CustomerId;
                    existingInvoice.SupplierId = invoice.SupplierId;
                    existingInvoice.InvoiceDate = invoice.InvoiceDate;
                    existingInvoice.DueDate = invoice.DueDate;
                    existingInvoice.DiscountAmount = invoice.DiscountAmount;
                    existingInvoice.PaidAmount = invoice.PaidAmount;
                    existingInvoice.Stage = invoice.Stage;

                    // 3. Sync Items
                    _context.InvoiceItems.RemoveRange(existingInvoice.InvoiceItems);
                    existingInvoice.InvoiceItems.Clear();

                    decimal totalAmount = 0;
                    decimal totalTax = 0;

                    foreach (var item in invoice.InvoiceItems)
                    {
                        if (item.ProductId.HasValue)
                        {
                            var product = await _productRepository.GetByIdAsync(item.ProductId.Value);
                            if (product == null) throw new Exception($"Product not found: {item.ProductId}");
                            item.ProductName = product.Name;
                            item.HSN = product.HSN;
                        }
                        else if (item.SubProductId.HasValue)
                        {
                            var subProduct = await _context.SubProducts.FindAsync(item.SubProductId.Value);
                            if (subProduct == null) throw new Exception($"Sub-Product not found: {item.SubProductId}");
                            item.ProductName = subProduct.Name;
                            item.HSN = "";
                        }
                        
                        decimal taxableValue = item.Quantity * item.UnitPrice;
                        decimal taxValue = (taxableValue * (item.GSTPercentage.HasValue ? item.GSTPercentage.Value : 0)) / 100;
                        item.TaxAmount = taxValue;
                        item.TotalAmount = taxableValue + taxValue;

                        totalAmount += taxableValue;
                        totalTax += taxValue;
                        
                        existingInvoice.InvoiceItems.Add(item);
                    }

                    // 4. Recalculate Totals
                    existingInvoice.TaxAmount = totalTax;
                    decimal subTotal = totalAmount + totalTax;
                    existingInvoice.GrandTotal = subTotal - existingInvoice.DiscountAmount;
                    decimal roundedTotal = Math.Round(existingInvoice.GrandTotal);
                    existingInvoice.RoundOff = roundedTotal - existingInvoice.GrandTotal;
                    existingInvoice.GrandTotal = roundedTotal;

                    // Validate PaidAmount
                    if (existingInvoice.PaidAmount < 0)
                        throw new Exception("Paid amount cannot be negative");
                    if (existingInvoice.PaidAmount > existingInvoice.GrandTotal)
                        throw new Exception("Paid amount cannot exceed grand total");

                    // Auto-calculate payment status
                    existingInvoice.Status = existingInvoice.PaymentStatus;

                    // 5. Apply new stock/balance if new stage is Invoice
                    if (existingInvoice.Stage == DocumentStage.Invoice)
                    {
                        foreach (var item in existingInvoice.InvoiceItems)
                        {
                            await UpdateProductAndSubProductStockAsync(item, existingInvoice.InvoiceType, false);
                        }

                        if (existingInvoice.CustomerId.HasValue)
                        {
                            var customer = await _customerRepository.GetByIdAsync(existingInvoice.CustomerId.Value);
                            if (customer != null) customer.CurrentBalance += (existingInvoice.GrandTotal - existingInvoice.PaidAmount);
                        }
                        else if (existingInvoice.SupplierId.HasValue)
                        {
                            var supplier = await _supplierRepository.GetByIdAsync(existingInvoice.SupplierId.Value);
                            if (supplier != null) supplier.CurrentBalance += (existingInvoice.GrandTotal - existingInvoice.PaidAmount);
                        }
                    }

                    _context.Invoices.Update(existingInvoice);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existingInvoice;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task AddPaymentTransactionAsync(int invoiceId, PaymentTransaction transaction)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) throw new Exception("Invoice not found");

            transaction.InvoiceId = invoiceId;
            
            // Validate payment amount (Basic validation, more complex logic handled by new balance)
            if (transaction.Amount <= 0)
                throw new Exception("Payment amount must be greater than zero");

            if (transaction.Amount > (invoice.GrandTotal - invoice.PaidAmount))
                throw new Exception("Payment amount cannot exceed remaining amount");

            // 1. Add Transaction
            await _context.PaymentTransactions.AddAsync(transaction);

            // 2. Update Invoice Paid Amount
            decimal oldPaidAmount = invoice.PaidAmount;
            invoice.PaidAmount += transaction.Amount;
            
            // 3. Update Status
            invoice.Status = invoice.PaymentStatus;

            // 4. Update Customer/Supplier Balance
            if (invoice.Stage == DocumentStage.Invoice)
            {
                if (invoice.CustomerId.HasValue)
                {
                    var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId.Value);
                    if (customer != null)
                    {
                        customer.CurrentBalance -= transaction.Amount;
                        await _customerRepository.UpdateAsync(customer);
                    }
                }
                else if (invoice.SupplierId.HasValue)
                {
                    var supplier = await _supplierRepository.GetByIdAsync(invoice.SupplierId.Value);
                    if (supplier != null)
                    {
                        supplier.CurrentBalance -= transaction.Amount;
                        await _supplierRepository.UpdateAsync(supplier);
                    }
                }
            }

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<PaymentTransaction>> GetPaymentTransactionsAsync(int invoiceId)
        {
            return await _context.PaymentTransactions
                .Where(pt => pt.InvoiceId == invoiceId)
                .OrderByDescending(pt => pt.PaymentDate)
                .ToListAsync();
        }

        public async Task UpdatePaymentAsync(int invoiceId, decimal newPaidAmount)
        {
            // Deprecated or redirect to transaction logic if possible. 
            // For now, keeping as is for backward compatibility but marking as needing refactor if used directly.
            // Ideally we should force using transactions. 
            // Let's refactor this to create a "Correction" transaction if we really want to keep it, 
            // OR just keep it as a manual override method.
            // Converting this to use manual override logic from previous implementation:
             var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) throw new Exception("Invoice not found");

            if (newPaidAmount < 0) throw new Exception("Paid amount cannot be negative");
            if (newPaidAmount > invoice.GrandTotal) throw new Exception("Paid amount cannot exceed grand total");

            decimal oldPaidAmount = invoice.PaidAmount;
            decimal difference = newPaidAmount - oldPaidAmount;

            invoice.PaidAmount = newPaidAmount;
            invoice.Status = invoice.PaymentStatus;

            if (invoice.Stage == DocumentStage.Invoice && difference != 0)
            {
                if (invoice.CustomerId.HasValue)
                {
                   var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId.Value);
                   if (customer != null)
                   {
                       customer.CurrentBalance -= difference;
                       await _customerRepository.UpdateAsync(customer);
                   }
                }
                else if (invoice.SupplierId.HasValue)
                {
                    var supplier = await _supplierRepository.GetByIdAsync(invoice.SupplierId.Value);
                    if (supplier != null)
                    {
                        supplier.CurrentBalance -= difference;
                        await _supplierRepository.UpdateAsync(supplier);
                    }
                }
            }

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInvoiceAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return;
                }

                if (invoice.Stage == DocumentStage.Invoice)
                {
                    foreach (var item in invoice.InvoiceItems)
                    {
                        await UpdateProductAndSubProductStockAsync(item, invoice.InvoiceType, true);
                    }

                    if (invoice.CustomerId.HasValue)
                    {
                        var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId.Value);
                        if (customer != null)
                        {
                            customer.CurrentBalance -= (invoice.GrandTotal - invoice.PaidAmount);
                        }
                    }
                    else if (invoice.SupplierId.HasValue)
                    {
                        var supplier = await _supplierRepository.GetByIdAsync(invoice.SupplierId.Value);
                        if (supplier != null)
                        {
                            supplier.CurrentBalance -= (invoice.GrandTotal - invoice.PaidAmount);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Invoice> UpdateStageAsync(int invoiceId, DocumentStage newStage)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) throw new Exception("Invoice not found");

            // If moving to Invoice stage for the first time, apply stock/balance updates
            if (invoice.Stage != DocumentStage.Invoice && newStage == DocumentStage.Invoice)
            {
                foreach (var item in invoice.InvoiceItems)
                {
                    await UpdateProductAndSubProductStockAsync(item, invoice.InvoiceType, false);
                }

                if (invoice.CustomerId.HasValue)
                {
                    var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId.Value);
                    if (customer != null) customer.CurrentBalance += (invoice.GrandTotal - invoice.PaidAmount);
                }
                else if (invoice.SupplierId.HasValue)
                {
                    var supplier = await _supplierRepository.GetByIdAsync(invoice.SupplierId.Value);
                    if (supplier != null) supplier.CurrentBalance += (invoice.GrandTotal - invoice.PaidAmount);
                }
            }

            invoice.Stage = newStage;
            await _context.SaveChangesAsync();
            return invoice;
        }
        private async Task UpdateProductAndSubProductStockAsync(InvoiceItem item, string invoiceType, bool isReverting)
        {
            decimal multiplier = (invoiceType == "Sale" ? -1 : 1) * (isReverting ? -1 : 1);

            if (item.ProductId.HasValue)
            {
                var product = await _context.Products
                    .Include(p => p.SubProductMappings)
                    .ThenInclude(m => m.SubProduct)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId.Value);

                if (product != null)
                {
                    product.StockQuantity += item.Quantity * multiplier;

                    foreach (var mapping in product.SubProductMappings)
                    {
                        if (mapping.SubProduct != null)
                        {
                            mapping.SubProduct.CurrentStock += (item.Quantity * mapping.RequiredQuantity) * multiplier;
                            _context.SubProducts.Update(mapping.SubProduct);
                        }
                    }
                    _context.Products.Update(product);
                }
            }
            else if (item.SubProductId.HasValue)
            {
                var subProduct = await _context.SubProducts.FindAsync(item.SubProductId.Value);
                if (subProduct != null)
                {
                    subProduct.CurrentStock += item.Quantity * multiplier;
                    _context.SubProducts.Update(subProduct);
                }
            }
        }
    }
}
