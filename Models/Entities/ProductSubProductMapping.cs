using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grevity.Models.Entities
{
    public class ProductSubProductMapping : BaseEntity, ICompanyEntity
    {
        public int? CompanyId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        [Required]
        [ForeignKey("SubProduct")]
        public int SubProductId { get; set; }
        public virtual SubProduct SubProduct { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Required quantity must be greater than 0")]
        public decimal RequiredQuantity { get; set; }
    }
}
