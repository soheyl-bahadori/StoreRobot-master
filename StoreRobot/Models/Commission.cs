using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreRobot.Models
{
    public class Commission
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CommissionPercent { get; set; }
        public int CategoryId { get; set; }
    }
}
