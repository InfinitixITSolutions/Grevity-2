using System;
using System.Collections.Generic;
using Grevity.Models.Entities;

namespace Grevity.Models.ViewModels
{
    public class InvoiceViewModel
    {
        public Invoice Invoice { get; set; }
        public IEnumerable<Customer> Customers { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<SubProduct> SubProducts { get; set; }
    }
}
