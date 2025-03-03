using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoreRobot.Models;

namespace StoreRobot.Utils
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

        public static Result NormalPriceChanging(Product product, DigiKalaApi digiKalaApi, int from, int to,
            int jumpStep, bool oldIsActive, CancellationToken token)
        {
            if (from >= to)
            {
                var updateResult = digiKalaApi.UpdateProductAsync(product.Dkpc,
                    product.DigiStockQuantity, true, 1, oldIsActive, from * 10).Result;
                if (updateResult.Status)
                {
                    return new Result() { Status = true };
                }
                updateResult = digiKalaApi.UpdateProductAsync(product.Dkpc,
                    product.DigiStockQuantity, true, 1, oldIsActive, to * 10).Result;
                if (!updateResult.Status)
                {
                    return new Result() { Status = false, Message = updateResult.Message };
                }
                for (int price = from; price >= to; price -= jumpStep)
                {
                    if (token.IsCancellationRequested)
                    {
                        return new Result() { Status = true };
                    }
                    updateResult = digiKalaApi.UpdateProductAsync(product.Dkpc,
                        product.DigiStockQuantity, true, 1, oldIsActive, price * 10).Result;
                    if (updateResult.Status)
                    {
                        return new Result() { Status = true };
                    }

                    if (price - jumpStep >= to || price == to) continue;
                    updateResult = digiKalaApi.UpdateProductAsync(product.Dkpc,
                        product.DigiStockQuantity, true, 1, oldIsActive, to * 10).Result;
                    if (updateResult.Status)
                    {
                        return new Result() { Status = true };
                    }
                }
                return new Result() { Status = false, Message = updateResult.Message };
            }
            return new Result() { Status = false, Message = "قیمت مصرف از حداقل قیمت کمتر است." };
        }
        public static double GetDigiPercent(int digiStatus)
        {
            if (digiStatus < 4 || digiStatus == 5)
                return 0;

            //var path = Utilities.GetSetupFilePath("Digi Percent.txt");
            //if (!File.Exists(path) ||
            //    string.IsNullOrEmpty(File.ReadAllText(path)))
            //{
            //    File.AppendAllText(path, $"{digiStatus}=15.49");
            //}
            //var lines = File.ReadAllLines(path).ToList();
            //return Convert.ToDouble(lines.FirstOrDefault(l => l.Trim().StartsWith(digiStatus.ToString()))?.Split('=')[1]);

            return Utilities.GetSettings().DigiPercent.Where(s=>s.Key == digiStatus).FirstOrDefault().Value;
        }
    }
}
 