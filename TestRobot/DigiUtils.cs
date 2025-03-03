using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestRobot
{
    public static class DigiUtils
    {
        public static int FindMinPrice(int domesticPrice, double commissionPercent, int minCommision, int maxCommision)
        {
            // x < minCommision
            var x = (domesticPrice + minCommision * 1.09) / (1 - 1.09 * commissionPercent);
            if (x * 0.07 <= minCommision)
            {
                return Convert.ToInt32(x);
            }

            // x > maxCommision
            x = (domesticPrice + maxCommision * 1.09) / (1 - 1.09 * commissionPercent);
            if (x * 0.07 >= maxCommision)
            {
                return Convert.ToInt32(x);
            }

            // minCommision < x < maxCommision
            x = domesticPrice / (0.9237 - 1.09 * commissionPercent);
            return Convert.ToInt32(x);
        }
    }
}
