using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grevity.Models.Entities
{
    public class Invoice : BaseEntity, ICompanyEntity
    {
        public int? CompanyId { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public string? InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        
        [ForeignKey("Customer")]
        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        [ForeignKey("Supplier")]
        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }
        
        public string InvoiceType { get; set; } = "Sale"; // Sale, Purchase
        
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal GrandTotal { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Paid amount cannot be negative")]
        public decimal PaidAmount { get; set; }
        
        // Computed property for remaining amount
        [NotMapped]
        public decimal RemainingAmount => GrandTotal - PaidAmount;
        
        // Computed property for automatic payment status
        [NotMapped]
        public string PaymentStatus
        {
            get
            {
                if (PaidAmount == 0) return "Unpaid";
                if (PaidAmount >= GrandTotal) return "Paid";
                return "Partial Paid";
            }
        }
        
        public bool IsGSTInvoice { get; set; } = true;
        public string Status { get; set; } = "Unpaid"; // Unpaid, Paid, Cancelled
        public DocumentStage Stage { get; set; } = DocumentStage.Invoice;
        
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}
