using System.ComponentModel.DataAnnotations;

namespace Grevity.Models.Entities
{
    public class Supplier : BaseEntity, ICompanyEntity
    {
        public int? CompanyId { get; set; }
        
        [Required(ErrorMessage = "Supplier Name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Address { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [Phone]
        public string Mobile { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? GSTIN { get; set; }

        public decimal OpeningBalance { get; set; } = 0;
        public decimal CurrentBalance { get; set; } = 0;
    }
}
