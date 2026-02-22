using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grevity.Models.Entities
{
    public class InvoiceItem : BaseEntity, ICompanyEntity
    {
        public int? CompanyId { get; set; }
        [ForeignKey("Invoice")]
        public int InvoiceId { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public virtual Invoice Invoice { get; set; }
        
        [ForeignKey("Product")]
        public int? ProductId { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public virtual Product Product { get; set; }

        [ForeignKey("SubProduct")]
        public int? SubProductId { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public virtual SubProduct SubProduct { get; set; }
        
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public string ProductName { get; set; } // Snapshot in case product name changes
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public string? HSN { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public decimal? GSTPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; } // (Qty * Price) + Tax
    }
}
