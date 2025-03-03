using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreRobot.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int StokeQuantity { get; set; }
        public string Sku { get; set; }
        public string Dkpc { get; set; }
        public int UserPrice { get; set; }
        public int ReferencePrice { get; set; }
        public bool IsPakhshOff { get; set; }
        public int PakhshPriceInOff { get; set; }
        public bool IsSafirOff { get; set; }
        public int SafirPriceInOff { get; set; }
        public int DigiStatus { get; set; }
        public int DomesticPrice { get; set; }
        public int JumpStep { get; set; }
        public int CommisionMin { get; set; }
        public int CommisionMax { get; set; }
        public string ExpiryDate { get; set; }
        public string Guarantee { get; set; }
        public int DigiStockQuantity { get; set; }
        public int PakhshAndSafirStockQuantity { get; set; }
    }
}
