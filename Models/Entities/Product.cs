using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grevity.Models.Entities
{
    public class Product : BaseEntity, ICompanyEntity
    {
        public int? CompanyId { get; set; }
        
        [Required(ErrorMessage = "Product Name is required")]
        [StringLength(200)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? HSN { get; set; }

        [StringLength(20)]
        public string? Unit { get; set; } = "Nos";
        
        public decimal? Price { get; set; } // Selling Price

        public decimal? PurchasePrice { get; set; } // Cost Price

        public decimal? GSTPercentage { get; set; } // Tax Rate

        public decimal StockQuantity { get; set; } = 0;

        public decimal LowStockAlertLimit { get; set; } = 0;

        public string ItemType { get; set; } = "Sales"; // "Sales" or "Purchase"

        // Navigation property for sub-product composition (for products made from sub products)
        public virtual ICollection<ProductSubProductMapping> SubProductMappings { get; set; } = new List<ProductSubProductMapping>();
    }
}
