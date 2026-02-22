using System.ComponentModel.DataAnnotations;

namespace Grevity.Models.Entities
{
    public class User : BaseEntity
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string PasswordHash { get; set; } // Simple hash for now
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string Role { get; set; } = "Admin";
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiryTime { get; set; }
    }
}
