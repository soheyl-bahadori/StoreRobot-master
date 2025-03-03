using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelUpdater.Models
{
    public static class Utilities
    {
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
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index));

        public static string GetColumnAddress(int columnNumber)
        {
            columnNumber += 3;
            StringBuilder columnAddress = new StringBuilder();

            while (columnNumber > 0)
            {
                int remainder = (columnNumber - 1) % 26;
                char ch = (char)('A' + remainder);
                columnAddress.Insert(0, ch);

                columnNumber = (columnNumber - 1) / 26;
            }

            return columnAddress.ToString();
        }
        public static bool IsNumeric(this string number)
        {
            return double.TryParse(number, out _);
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
                string jsonString = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, jsonString);
                return settings;
            }
        }
    }
}
