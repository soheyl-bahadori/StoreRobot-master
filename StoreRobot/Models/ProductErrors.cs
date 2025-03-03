using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreRobot.Models
{
    public class ProductErrors
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SKU { get; set; }
        public string ErrorMessage { get; set; }
        public StoreEnum Store { get; set; }
        public string? ErroredPrice { get; set; }
        public string? FirstPrice { get; set; }
        public string? Stock { get; set; }
        public string? UpdateError { get; set; }
        public int ReportNumber { get; set; }
    }
}
