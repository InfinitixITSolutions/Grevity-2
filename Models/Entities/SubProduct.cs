using System.ComponentModel.DataAnnotations;

namespace Grevity.Models.Entities
{
    public class SubProduct : BaseEntity, ICompanyEntity
    {
        public int? CompanyId { get; set; }
        
        [Required(ErrorMessage = "Sub Product Name is required")]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = "Pcs"; // Kg, Pcs, Liter, Meter, etc.
        
        public decimal CurrentStock { get; set; } = 0;

        public decimal? PurchasePrice { get; set; }

        public decimal LowStockAlertLimit { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
