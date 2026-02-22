using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grevity.Models.Entities
{
    public class UserCompany : BaseEntity
    {
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public int BusinessSettingId { get; set; } // This acts as CompanyId
        [ForeignKey("BusinessSettingId")]
        public BusinessSetting Company { get; set; }

        public bool IsDefault { get; set; }
    }
}
