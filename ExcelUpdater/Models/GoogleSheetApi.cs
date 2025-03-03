using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using ExcelUpdater.Models;
using Data = Google.Apis.Sheets.v4.Data;

namespace ExcelUpdater.Models
{
    public class GoogleSheetApi
    {
        private SheetsService _service;
        const string spreadsheetId = "1rZpvxMARcHQ9pq2JLAsfxSAuprLM51NOMn0PZIXkd-0";

        public GoogleSheetApi()
        {
            string credentialsString = "SafirCredentials.json";
            GoogleCredential credential;
            string[] scopes = { SheetsService.Scope.Spreadsheets };
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(credentialsString));
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleCredential.FromStream(stream).CreateScoped(scopes);
            }

            // Create Google Sheets API service.
            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Rahkar Mofid Robot",
            });
        }

        /// <summary>
        /// Available when DbContext exist
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> EditStockQuantityCell(string productId, int triggerValue)
        {
            //fooooooor
            try
            {
                //find rows
                var getRequest = _service.Spreadsheets.Values.Get(spreadsheetId, $"Pakhsh!C2:ZZ");
                var rows = (await getRequest.ExecuteAsync()).Values;

                //column Id number
                var columnData = Utilities.GetColumnData(rows[0]);
                int idColumnNumber = columnData["1014"];

                //find default row and trigger value
                var editingRows = new List<IList<object>>();
                editingRows.Add(rows.FirstOrDefault(r => r.Count >= idColumnNumber + 1 && r[idColumnNumber] != null
                                && r[idColumnNumber].ToString().Trim().Replace("'", "") == productId.Trim()));
                var triggerValues = new List<int>() { triggerValue };

                if ((string)editingRows.FirstOrDefault()[columnData["1003"]] != "0" && 
                    (string)editingRows.FirstOrDefault()[columnData["1004"]] != "0")
                {
                    triggerValues = new List<int>();
                    foreach (var (rowAddress, index) in ((string)editingRows.FirstOrDefault()[columnData["1003"]]).Split(',').WithIndex())
                    {
                        editingRows.Add(rows.FirstOrDefault(r => r[columnData["1002"]].ToString()?.Trim() == rowAddress.Trim()));
                        var triggerV = triggerValue *
                             Convert.ToInt32(((string)editingRows.FirstOrDefault()[columnData["1004"]]).Split(',')[index].Trim());
                        triggerValues.Add(triggerV);
                    }
                    editingRows.RemoveAt(0);
                }

                if (editingRows.Count != triggerValues.Count)
                    throw new Exception("The parrents count must equal to trigger values count!");

                var returnJson = new List<string>();
                foreach (var (row, index) in editingRows.WithIndex())
                {
                    int rowIndex = rows.IndexOf(row) + 2;

                    var stockRowNum = columnData["1001"];

                    //find stock quantity
                    var stokeQuantity = !string.IsNullOrEmpty(row[stockRowNum].ToString().Trim())
                        ? Convert.ToInt32(Convert.ToDouble(row[stockRowNum]))
                        : 0;

                    //check trigger and stock
                    /*if (stokeQuantity == 0 && triggerValue < 0)
                        return;*/

                    //edit row
                    string range = $"Pakhsh!{Utilities.GetColumnAddress(stockRowNum)}{rowIndex}";
                    SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum valueInputOption =
                        SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    Data.ValueRange requestBody = new Data.ValueRange()
                    {
                        Values = new List<IList<object>>()
                        {
                            new List<object>()
                            {
                                (stokeQuantity + triggerValues[index]).ToString().Replace("'", "")
                            }
                        }
                    };

                    SpreadsheetsResource.ValuesResource.UpdateRequest request =
                        _service.Spreadsheets.Values.Update(requestBody, spreadsheetId, range);
                    request.ValueInputOption = valueInputOption;

                    for (int j = 0; j < 5; j++)
                    {
                        try
                        {
                            Data.UpdateValuesResponse response = await request.ExecuteAsync();
                            break;
                        }
                        catch (Exception)
                        {
                            if (j == 4)
                                throw;
                            Thread.Sleep(1000);
                        }
                    }

                    var getRequest2 = _service.Spreadsheets.Values.Get(spreadsheetId, $"Pakhsh!C{rowIndex}:ZZ{rowIndex}");
                    var rows2 = (await getRequest2.ExecuteAsync()).Values;

                    returnJson.Add(GetProductJson(rows2[0], columnData));
                }

                return returnJson;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string GetProductJson(IList<object> row, Dictionary<string, int> columnData)
        {

            var sku = row[columnData["1002"]].ToString() ?? string.Empty;

            var dkpc = string.IsNullOrEmpty(row[columnData["1014"]].ToString()) ? string.Empty :
                                row[columnData["1014"]].ToString().Trim();

            var userPrice = !string.IsNullOrEmpty(row[columnData["1005"]].ToString().Trim())
                                ? Convert.ToInt32(row[columnData["1005"]].ToString()!.Replace(",", ""))
                                : 0;
            var isPakhshOff = !string.IsNullOrEmpty(row[columnData["1009"]].ToString().Trim())
                                ? Convert.ToBoolean(row[columnData["1009"]])
                                : false;

            var pakhshPriceInOff = !string.IsNullOrEmpty(row[columnData["1007"]].ToString().Trim())
                ? Convert.ToInt32(row[columnData["1007"]].ToString()!.Replace(",", ""))
                : 0;

            var expiryDate = string.IsNullOrEmpty(row[columnData["1008"]].ToString()) ? string.Empty :
                row[columnData["1008"]].ToString().Trim();

            var isSafirOff = !string.IsNullOrEmpty(row[columnData["1010"]].ToString().Trim())
                                ? Convert.ToBoolean(row[columnData["1010"]])
                                : false;

            var safirPriceInOff = !string.IsNullOrEmpty(row[columnData["1013"]].ToString().Trim())
                ? Convert.ToInt32(row[columnData["1013"]].ToString()!.Replace(",", ""))
                : 0;

            var guarantee = !string.IsNullOrEmpty(row[columnData["1018"]].ToString()) ?
                                row[columnData["1018"]].ToString().Trim() :
                                string.Empty;

            int stockQuantity = !string.IsNullOrEmpty(row[columnData["1001"]].ToString().Trim()) 
                            && row[columnData["1001"]].ToString().Trim().IsNumeric()
                                ? (int)Convert.ToDouble(row[columnData["1001"]])
                                : 0;

            int digiStockQuantity = !string.IsNullOrEmpty(row[columnData["1019"]].ToString().Trim())
                            && row[columnData["1019"]].ToString().Trim().IsNumeric()
                                ? (int)Convert.ToDouble(row[columnData["1019"]])
                                : 0;

            int pakhshAndSafirStockQuantity = !string.IsNullOrEmpty(row[columnData["1020"]].ToString().Trim())
                            && row[columnData["1020"]].ToString().Trim().IsNumeric()
                                ? (int)Convert.ToDouble(row[columnData["1020"]])
                                : 0;
            return $"{{\"safirPriceInOff\":{safirPriceInOff},\"guarantee\":\"{guarantee}\",\"isPakhshChanged\":true,\"isSafirChanged\":true,\"expiryDate\":\"{expiryDate}\",\"isDigiChanged\":true,\"isPakhshOff\":{isPakhshOff.ToString().ToLowerInvariant()},\"dkpc\":\"{dkpc}\",\"price\":{userPrice},\"stock\":{stockQuantity},\"digiStock\":{digiStockQuantity},\"pakhshAndSafirStock\":{pakhshAndSafirStockQuantity},\"sku\":\"{sku}\",\"isSafirOff\":{isSafirOff.ToString().ToLowerInvariant()},\"pakhshPriceInOff\":{pakhshPriceInOff}}}";
        }

        public async Task<List<string>> GetChildrenProducts(string sku)
        {
            //find row with sku
            var getRequest = _service.Spreadsheets.Values.Get(spreadsheetId, $"Pakhsh!C2:ZZ");
            var rows = (await getRequest.ExecuteAsync()).Values;
            var columnData = Utilities.GetColumnData(rows[0]);
            var childrenRows = rows.Where(r => r[columnData["1003"]] != null 
                                            && (string)r[columnData["1003"]] != "0" && 
                                            CheckStringContains(sku.Trim(), r[columnData["1003"]].ToString().Trim().Replace("'", "")));

            List<string> childrenJsons = new();
            foreach (var row in childrenRows)
            {
                childrenJsons.Add(GetProductJson(row, columnData));
            }

            return childrenJsons;
        }
        private bool CheckStringContains(string inputString, string targetString)
        {
            // Special case: If the target string is exactly the input string, return true
            if (targetString == inputString)
            {
                return true;
            }

            // Handle other cases
            string pattern = $@"(\b|\W){Regex.Escape(inputString)}(\b|\W)";
            return Regex.IsMatch(targetString, pattern, RegexOptions.IgnoreCase);
        }
    }
}
