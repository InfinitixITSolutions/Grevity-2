using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Grevity.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;

namespace Grevity.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> SalesReport(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Invoices
                .Include(i => i.Customer)
                .Where(i => i.InvoiceType == "Sale");

            if (fromDate.HasValue) query = query.Where(i => i.InvoiceDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(i => i.InvoiceDate <= toDate.Value);

            var sales = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync();
            
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(sales);
        }

        public IActionResult ExportSalesInternal(DateTime? fromDate, DateTime? toDate)
        {
             // Logic to export excel
             var query = _context.Invoices
                .Include(i => i.Customer)
                .Where(i => i.InvoiceType == "Sale");

            if (fromDate.HasValue) query = query.Where(i => i.InvoiceDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(i => i.InvoiceDate <= toDate.Value);

            var sales = query.OrderByDescending(i => i.InvoiceDate).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sales Report");
                worksheet.Cell(1, 1).Value = "Invoice #";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Customer";
                worksheet.Cell(1, 4).Value = "GSTIN";
                worksheet.Cell(1, 5).Value = "Tax Amount";
                worksheet.Cell(1, 6).Value = "Total Amount";

                int row = 2;
                foreach (var item in sales)
                {
                    worksheet.Cell(row, 1).Value = item.InvoiceNumber;
                    worksheet.Cell(row, 2).Value = item.InvoiceDate;
                    worksheet.Cell(row, 3).Value = item.Customer?.Name;
                    worksheet.Cell(row, 4).Value = item.Customer?.GSTIN;
                    worksheet.Cell(row, 5).Value = item.TaxAmount;
                    worksheet.Cell(row, 6).Value = item.GrandTotal;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesReport.xlsx");
                }
            }
        }

        public async Task<IActionResult> PurchaseReport(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Invoices
                .Include(i => i.Supplier)
                .Where(i => i.InvoiceType == "Purchase");

            if (fromDate.HasValue) query = query.Where(i => i.InvoiceDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(i => i.InvoiceDate <= toDate.Value);

            var purchases = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync();
            
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(purchases);
        }

        public async Task<IActionResult> CustomerOutstanding()
        {
            var customers = await _context.Customers
                .Where(c => c.CurrentBalance != 0) // Filter only customers with outstanding balance
                .OrderByDescending(c => c.CurrentBalance)
                .ToListAsync();
            
            return View(customers);
        }

        public async Task<IActionResult> ConsolidatedReport()
        {
            // Financial Summary
            var totalReceivable = await _context.Customers.SumAsync(c => c.CurrentBalance);
            var totalPayable = await _context.Suppliers.SumAsync(s => s.CurrentBalance);
            
            var totalReceived = await _context.Invoices
                .Where(i => i.InvoiceType == "Sale")
                .SumAsync(i => i.PaidAmount);
                
            var totalPaid = await _context.Invoices
                .Where(i => i.InvoiceType == "Purchase")
                .SumAsync(i => i.PaidAmount);

            var totalSales = await _context.Invoices
                .Where(i => i.InvoiceType == "Sale" && i.Stage == Models.Entities.DocumentStage.Invoice)
                .SumAsync(i => i.GrandTotal);

            var totalPurchases = await _context.Invoices
                .Where(i => i.InvoiceType == "Purchase" && i.Stage == Models.Entities.DocumentStage.Invoice)
                .SumAsync(i => i.GrandTotal);

            // Top Debtors (Customers who owe us)
            var topCustomers = await _context.Customers
                .Where(c => c.CurrentBalance > 0)
                .OrderByDescending(c => c.CurrentBalance)
                .Take(5)
                .ToListAsync();

            // Top Creditors (Suppliers we owe)
            var topSuppliers = await _context.Suppliers
                .Where(s => s.CurrentBalance > 0)
                .OrderByDescending(s => s.CurrentBalance)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalReceivable = totalReceivable;
            ViewBag.TotalPayable = totalPayable;
            ViewBag.TotalReceived = totalReceived;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalPurchases = totalPurchases;
            ViewBag.TopCustomers = topCustomers;
            ViewBag.TopSuppliers = topSuppliers;

            return View();
        }

        public async Task<IActionResult> UnpaidSales()
        {
            var unpaidInvoices = await _context.Invoices
                .Include(i => i.Customer)
                .Where(i => i.InvoiceType == "Sale" 
                         && i.Stage == Models.Entities.DocumentStage.Invoice
                         && i.PaidAmount < i.GrandTotal)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();

            return View(unpaidInvoices);
        }

        public async Task<IActionResult> UnpaidPurchase()
        {
            var unpaidBills = await _context.Invoices
                .Include(i => i.Supplier)
                .Where(i => i.InvoiceType == "Purchase" 
                         && i.Stage == Models.Entities.DocumentStage.Invoice
                         && i.PaidAmount < i.GrandTotal)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();

            return View(unpaidBills);
        }

        public async Task<IActionResult> PaidPurchase()
        {
            var paidBills = await _context.Invoices
                .Include(i => i.Supplier)
                .Where(i => i.InvoiceType == "Purchase" 
                         && i.Stage == Models.Entities.DocumentStage.Invoice
                         && i.PaidAmount >= i.GrandTotal)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return View(paidBills);
        }


    }
}
