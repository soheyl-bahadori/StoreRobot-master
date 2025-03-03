using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreRobot.Models
{
    public class BuyBoxResult
    {
        public bool Result { get; set; }
        public bool IsBuyBoxWinner { get; set; }
        public bool HaveCompetitor { get; set; }
        public int LeastPrice { get; set; }
        public string ErrorMessage { get; set; }
    }
}
