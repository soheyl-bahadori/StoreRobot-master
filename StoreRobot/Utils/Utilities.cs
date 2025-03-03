using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using StoreRobot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StoreRobot.Utils
{
    public static class Utilities
    {
        public static bool IsProductChanged(string jsonWebsiteProduct, Product sheetProduct, StoreEnum store)
        {
            if (store == StoreEnum.DigiKala)
            {
                if (sheetProduct == null)
                    return false;

                var websiteProduct = (JObject)JsonConvert.DeserializeObject(jsonWebsiteProduct);

                // Price Difference;


                // stock difference
                var stockQuantity = (int)websiteProduct["stock"]["seller_stock"];

                if (stockQuantity == 0 && sheetProduct.DigiStockQuantity == 0)
                    return false;

                return sheetProduct.DigiStockQuantity != stockQuantity;
            }
            else
            {
                if (sheetProduct == null)
                    return false;
                if (sheetProduct.IsSafirOff && sheetProduct.SafirPriceInOff == 0)
                    sheetProduct.IsSafirOff = false;
                if (sheetProduct.IsPakhshOff && sheetProduct.PakhshPriceInOff == 0)
                    sheetProduct.IsPakhshOff = false;

                var websiteProduct = (JObject)JsonConvert.DeserializeObject(jsonWebsiteProduct);
                if ((websiteProduct["stock_quantity"].Type == JTokenType.Null ||
                     (int)websiteProduct["stock_quantity"] == 0) &&
                    sheetProduct.PakhshAndSafirStockQuantity == 0)
                    return false;

                bool offPriceDifference;
                if (store == StoreEnum.Pakhsh)
                    offPriceDifference =
                        sheetProduct.IsPakhshOff == string.IsNullOrEmpty((string)websiteProduct["sale_price"]) ||
                        sheetProduct.PakhshPriceInOff.ToString() != (string)websiteProduct["sale_price"] &&
                        sheetProduct.IsPakhshOff;
                else
                    offPriceDifference =
                        sheetProduct.IsSafirOff == string.IsNullOrEmpty((string)websiteProduct["sale_price"]) ||
                        sheetProduct.SafirPriceInOff.ToString() != (string)websiteProduct["sale_price"] &&
                        sheetProduct.IsSafirOff;

                var stockQuantity = websiteProduct["stock_quantity"].Type == JTokenType.Null
                    ? 0
                    : (int)websiteProduct["stock_quantity"];

                var attributes = (JArray)websiteProduct["attributes"];

                //expiry
                int expiryId = store == StoreEnum.SafirKala ? 717 : 739;
                var expiryObj = attributes.FirstOrDefault(a => (int)a["id"] == expiryId);
                bool isExpiryChanged = false;
                if (expiryObj != null)
                {
                    string expiry = (string)websiteProduct["type"] == "variation" ? (string)expiryObj["option"]
                                   : (string)((JArray)expiryObj["options"]).FirstOrDefault();
                    if (string.IsNullOrEmpty(expiry))
                        expiry = "";
                    if (expiry != sheetProduct.ExpiryDate)
                    {
                        isExpiryChanged = true;
                    }
                }
                else if (!string.IsNullOrEmpty(sheetProduct.ExpiryDate))
                {
                    isExpiryChanged = true;
                }

                //guarantee
                int guaranteeId = store == StoreEnum.SafirKala ? 6 : 3;
                var guaranteeObj = attributes.FirstOrDefault(a => (int)a["id"] == guaranteeId);
                bool isGuaranteeChanged = false;
                if (guaranteeObj != null)
                {
                    string guarantee = (string)websiteProduct["type"] == "variation" ? (string)guaranteeObj["option"]
                                   : (string)((JArray)guaranteeObj["options"]).FirstOrDefault();
                    if (string.IsNullOrEmpty(guarantee))
                        guarantee = "";
                    if (guarantee != sheetProduct.Guarantee)
                    {
                        isGuaranteeChanged = true;
                    }
                }
                else if (!string.IsNullOrEmpty(sheetProduct.Guarantee))
                {
                    isGuaranteeChanged = true;
                }

                return sheetProduct.PakhshAndSafirStockQuantity != stockQuantity ||
                       sheetProduct.UserPrice.ToString() != (string)websiteProduct["regular_price"] ||
                       offPriceDifference || isExpiryChanged || isGuaranteeChanged;
            }
        }


        public static string ToFarsi(this string number)
        {
            return number.Replace("0", "۰")
                .Replace("1", "۱")
                .Replace("2", "۲")
                .Replace("3", "۳")
                .Replace("4", "۴")
                .Replace("5", "۵")
                .Replace("6", "۶")
                .Replace("7", "۷")
                .Replace("8", "۸")
                .Replace("9", "۹");
        }

        public static string ToEnglish(this string number)
        {
            return number.Replace("۰", "0")
                .Replace("۱", "1")
                .Replace("۲", "2")
                .Replace("۳", "3")
                .Replace("۴", "4")
                .Replace("۵", "5")
                .Replace("۶", "6")
                .Replace("۷", "7")
                .Replace("۸", "8")
                .Replace("۹", "9");
        }

        public static bool IsElementPresent(IWebDriver driver, By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public static bool IsNumeric(this string number)
        {
            return double.TryParse(number, out _);
        }
        public static void ScrollToElement(IWebDriver driver, IWebElement element)
        {
            Actions actions = new Actions(driver);
            actions.MoveToElement(element).Perform();
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index));

        public static int ZeroConvert(this int number, int zeroCount)
        {
            var num = Math.Pow(10, zeroCount);

            number = Convert.ToInt32(Math.Ceiling(number / num)) * Convert.ToInt32(num);

            return number;
            /*var zero = "";
            for (int i = 0; i < zeroCount; i++)
            {
                zero += "0";
            }
            return Convert.ToInt32(number.ToString().Remove(number.ToString().Length - zeroCount) + zero);*/
        }

        public static string ChangeAttributes(string productJson, string newDate, string newGuarantee, StoreEnum store, bool hasParent, bool hasVariation)
        {
            var productObj = (JObject)JsonConvert.DeserializeObject(productJson);
            var attributes = (JArray)productObj["attributes"];

            //expiry
            int expiryId = store == StoreEnum.SafirKala ? 717 : 739;
            var expiryAttribute = attributes.FirstOrDefault(a => (int)a["id"] == expiryId);
            if (expiryAttribute != null && !hasVariation)
            {
                attributes.Remove(expiryAttribute);
            }
            if (!string.IsNullOrEmpty(newDate))
            {
                if (hasVariation)
                    attributes.Remove(expiryAttribute);
                var newAttribute = new JObject();
                newAttribute["id"] = expiryId;
                if (!hasParent)
                {
                    newAttribute["visible"] = true;
                    newAttribute["variation"] = hasVariation;
                }
                if (hasParent)
                    newAttribute["option"] = "";
                else
                    newAttribute["options"] = expiryAttribute != null && hasVariation ? expiryAttribute["options"]
                        : new JArray();
                expiryAttribute = newAttribute;

                if (hasParent)
                {
                    expiryAttribute["option"] = newDate;
                }
                else
                {
                    var options = (JArray)expiryAttribute["options"];
                    options.Add(newDate);
                    expiryAttribute["options"] = options;
                }
                attributes.Add(expiryAttribute);
            }


            //guarantee
            int guaranteeId = store == StoreEnum.SafirKala ? 6 : 3;
            var guaranteeAttribute = attributes.FirstOrDefault(a => (int)a["id"] == guaranteeId);
            if (guaranteeAttribute != null && !hasVariation)
            {
                attributes.Remove(guaranteeAttribute);
            }
            if (!string.IsNullOrEmpty(newGuarantee))
            {
                if (hasVariation)
                    attributes.Remove(guaranteeAttribute);
                var newAttribute = new JObject();
                newAttribute["id"] = guaranteeId;
                if (!hasParent)
                {
                    newAttribute["visible"] = true;
                    newAttribute["variation"] = hasVariation;
                }
                if (hasParent)
                    newAttribute["option"] = "";
                else
                    newAttribute["options"] = guaranteeAttribute != null && hasVariation ? guaranteeAttribute["options"] : new JArray();
                guaranteeAttribute = newAttribute;

                if (hasParent)
                {
                    guaranteeAttribute["option"] = newGuarantee;
                }
                else
                {
                    var options = (JArray)guaranteeAttribute["options"];
                    options.Add(newGuarantee);
                    guaranteeAttribute["options"] = options;
                }
                attributes.Add(guaranteeAttribute);
            }

            return JsonConvert.SerializeObject(attributes);
        }
        public static Dictionary<string, int> GetColumnData(IList<object> row)
        {
            var data = new Dictionary<string, int>();
            foreach (var colId in row)
            {
                if (!string.IsNullOrEmpty(colId.ToString()))
                {
                    data[colId.ToString()] = row.IndexOf(colId);
                }
            }
            return data;
        }

        public static string GetSetupFilePath(string fileName)
        {
            var folderPath = Environment.GetFolderPath(
                        System.Environment.SpecialFolder.DesktopDirectory) + "\\setup folder";

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var path = Path.Combine(folderPath, fileName);
            return path;
        }

        public static SetupSettings GetSettings()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\setup folder";
            string filePath = Path.Combine(desktopPath, "setupSettings.json");

            // Check if the file exists
            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                SetupSettings settings = JsonConvert.DeserializeObject<SetupSettings>(jsonString);
                return settings;
            }
            else
            {
                //If not Exist Add File with Default Values
                SetupSettings settings = new SetupSettings();

                settings.DigiPercent = new List<(int Key, double Value)>
                    {
                        ( 4,15.496),
                        ( 6, 22 ),
                        ( 7, 20 ),
                        ( 8, 33 ),
                        ( 9, 20 )
                    };

                string jsonString = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, jsonString);
                return settings;
            }
        }

        public static bool EditSettings(SetupSettings setupSettings)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\setup folder";
            string filePath = Path.Combine(desktopPath, "setupSettings.json");


            string modifiedJsonString = JsonConvert.SerializeObject(setupSettings, Formatting.Indented);

            try
            {
                File.WriteAllText(filePath, modifiedJsonString);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
    }
}