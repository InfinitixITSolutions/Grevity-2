using System;
using System.ComponentModel.DataAnnotations;

namespace Grevity.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? CompanyId { get; set; }
        public string Action { get; set; } // Create, Update, Delete
        public string EntityName { get; set; } // Product, Invoice, etc.
        public string EntityId { get; set; } // PK of the entity
        public string Details { get; set; } // JSON or description
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
