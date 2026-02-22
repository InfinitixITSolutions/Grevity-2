using System.ComponentModel.DataAnnotations;

namespace Grevity.Models.Entities
{
    public class BusinessSetting : BaseEntity
    {
        [Required]
        public string CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        [Phone]
        public string? Phone { get; set; }
        [Phone]
        public string? Mobile { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? EmailPassword { get; set; }
        public string? GSTIN { get; set; }
        public string? LogoPath { get; set; }
        public bool IsGSTEnabled { get; set; } = true;
        public decimal DefaultGSTPercentage { get; set; } = 18.00m;
        public string CurrentFinancialYear { get; set; } // e.g., "2023-2024"
    }
}
