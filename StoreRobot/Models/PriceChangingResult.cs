using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreRobot.Models
{
    public enum ChangingErrors
    {
        MinimumReached,
        MaximumReached,
        ErrorOccurred,
        NotLoaded
    }
    public class PriceChangingResult
    {
        public bool Status { get; set; }
        public ChangingErrors ChangingErrors { get; set; }
        public int LastPrice { get; set; }
        public string ErrorMessage { get; set; }
    }
}
