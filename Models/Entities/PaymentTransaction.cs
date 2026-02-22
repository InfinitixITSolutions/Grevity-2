using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grevity.Models.Entities
{
    public class PaymentTransaction : BaseEntity, ICompanyEntity
    {
        public int? CompanyId { get; set; }

        [ForeignKey("Invoice")]
        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Bank Transfer, Cheque
        
        public string? ReferenceNumber { get; set; } // Cheque No, Transaction ID
        
        public string? Notes { get; set; }
    }
}
