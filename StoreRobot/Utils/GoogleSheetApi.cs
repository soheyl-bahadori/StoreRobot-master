using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using StoreRobot.Models;

namespace StoreRobot.Utils
{
    public class GoogleSheetApi
    {
        private ApplicationDbContext? _db;
        private SheetsService _service;
        const string spreadsheetId = "1rZpvxMARcHQ9pq2JLAsfxSAuprLM51NOMn0PZIXkd-0";
        //const string spreadsheetId = "1SNNu-kBOchtjqk_lJOsxrZoqdQRo0ITiQgrplI_FuPM";

        public GoogleSheetApi(ApplicationDbContext? db = null)
        {
            WooCommerceApi testApi = new(StoreEnum.SafirKala);
            while (!testApi.ServerRunningAsync().Result)
            {
                var serverError = new ConnectionError(testApi);
                serverError.ShowDialog();
            }
            _db = db;
            GoogleCredential credential;
            string[] scopes = { SheetsService.Scope.Spreadsheets };
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith("credentials.json"));
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
        public async Task<Result> AddSheetToDbAsync()
        {
            var result = await Task.Run(() =>
            {
                try
                {
                    WooCommerceApi testApi = new(StoreEnum.SafirKala);
                    while (!testApi.ServerRunningAsync().Result)
                    {
                        var serverError = new ConnectionError(testApi);
                        serverError.ShowDialog();
                    }
                    _db.Products.RemoveRange(_db.Products.ToList());
                    _db.SaveChanges();
                    string range = "Pakhsh!C2:ZZ";
                    var request = _service.Spreadsheets.Values.Get(spreadsheetId, range);
                    IList<IList<object>> rows = new List<IList<object>>();
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            rows = request.Execute().Values;
                            break;
                        }
                        catch (Exception)
                        {
                            if (i == 4)
                                throw;
                        }
                    }
                    var products = new List<Product>();
                    /////////test
                    var setting = Utilities.GetSettings().DigiTest;
                    //var path = Utilities.GetSetupFilePath("Digi Test.txt");
                    //if (!File.Exists(path) || string.IsNullOrEmpty(File.ReadAllText(path)))
                    //{
                    //    File.AppendAllText(path, $"3\n0");
                    //}
                    //int from = Convert.ToInt32(File.ReadAllLines(path)[0]) - 1;
                    //int length = Convert.ToInt32(File.ReadAllLines(path)[1]);
                    int from = setting.From - 1;
                    int length = setting.Count;
                    if (from < 2)
                        from = 2;
                    if (length == 0)
                    {
                        from = 2;
                        length = rows.Count;
                    }
                    /////////test
                    var columnData = Utilities.GetColumnData(rows[0]);
                    foreach (var (row, index) in rows.Skip(from - 1).Take(length).WithIndex())
                    {
                        try
                        {
                            var product = new Product();

                            product.Name = string.IsNullOrEmpty(row[columnData["1016"]].ToString()) ? string.Empty :
                                row[columnData["1016"]].ToString().Trim();

                            product.StokeQuantity = !string.IsNullOrEmpty(row[columnData["1001"]].ToString().Trim()) && 
                                row[columnData["1001"]].ToString().Trim().IsNumeric()
                                ? (int)Convert.ToDouble(row[columnData["1001"]])
                                : 0;
                            if (product.StokeQuantity < 0) product.StokeQuantity = 0;

                            product.DigiStockQuantity = !string.IsNullOrEmpty(row[columnData["1019"]].ToString().Trim()) &&
                                row[columnData["1019"]].ToString().Trim().IsNumeric()
                                ? (int)Convert.ToDouble(row[columnData["1019"]])
                                : 0;
                            if (product.DigiStockQuantity < 0) product.DigiStockQuantity = 0;

                            product.PakhshAndSafirStockQuantity = !string.IsNullOrEmpty(row[columnData["1020"]].ToString().Trim()) &&
                                row[columnData["1020"]].ToString().Trim().IsNumeric()
                                ? (int)Convert.ToDouble(row[columnData["1020"]])
                                : 0;
                            if (product.PakhshAndSafirStockQuantity < 0) product.PakhshAndSafirStockQuantity = 0;

                            product.Sku = row[columnData["1002"]].ToString() ?? string.Empty;

                            product.Dkpc = string.IsNullOrEmpty(row[columnData["1014"]].ToString()) ? string.Empty :
                                row[columnData["1014"]].ToString().Trim();

                            product.UserPrice = !string.IsNullOrEmpty(row[columnData["1005"]].ToString().Trim())
                                ? Convert.ToInt32(row[columnData["1005"]].ToString()!.Replace(",", ""))
                                : 0;

                            product.ReferencePrice = !string.IsNullOrEmpty(row[columnData["1006"]].ToString().Trim())
                                ? Convert.ToInt32(row[columnData["1006"]].ToString()!.Replace(",", ""))
                                : 0;

                            product.IsPakhshOff = !string.IsNullOrEmpty(row[columnData["1009"]].ToString().Trim())
                                ? Convert.ToBoolean(row[columnData["1009"]])
                                : false;

                            product.PakhshPriceInOff = !string.IsNullOrEmpty(row[columnData["1007"]].ToString().Trim())
                                ? Convert.ToInt32(row[columnData["1007"]].ToString()!.Replace(",", ""))
                                : 0;

                            product.ExpiryDate = string.IsNullOrEmpty(row[columnData["1008"]].ToString()) ? string.Empty :
                                row[columnData["1008"]].ToString().Trim();

                            product.IsSafirOff = !string.IsNullOrEmpty(row[columnData["1010"]].ToString().Trim())
                                ? Convert.ToBoolean(row[columnData["1010"]])
                                : false;

                            product.SafirPriceInOff = !string.IsNullOrEmpty(row[columnData["1013"]].ToString().Trim())
                                ? Convert.ToInt32(row[columnData["1013"]].ToString()!.Replace(",", ""))
                                : 0;

                            product.DigiStatus = !string.IsNullOrEmpty(row[columnData["1011"]].ToString().Trim()) &&
                                                 int.TryParse(row[columnData["1011"]].ToString().Trim(), out _)
                                ? Convert.ToInt32(row[columnData["1011"]].ToString())
                                : 0;

                            product.DomesticPrice = !string.IsNullOrEmpty(row[columnData["1012"]].ToString().Trim())
                                ? Convert.ToInt32(row[columnData["1012"]].ToString()!.Replace(",", ""))
                                : 0;

                            product.JumpStep = !string.IsNullOrEmpty(row[columnData["1015"]].ToString())
                                ? Convert.ToInt32(row[columnData["1015"]].ToString()!.Replace(",", ""))
                                : 100;

                            product.CommisionMin = !string.IsNullOrEmpty(row[columnData["1017"]].ToString().Trim())
                                ? Convert.ToInt32(row[columnData["1017"]].ToString()!.Split('-')[0])
                                : 9000;

                            product.CommisionMax = !string.IsNullOrEmpty(row[columnData["1017"]].ToString().Trim())
                                ? Convert.ToInt32(row[columnData["1017"]].ToString()!.Split('-')[1])
                                : 80000;
                            product.Guarantee = !string.IsNullOrEmpty(row[columnData["1018"]].ToString()) ?
                                row[columnData["1018"]].ToString().Trim() :
                                "اصالت و سلامت فیزیکی کالا";

                            products.Add(product);
                        }
                        catch (Exception e)
                        {
                            var problemPath = Utilities.GetSetupFilePath("problems.txt");
                            File.AppendAllText(problemPath,
                                index + 2 + $"------{e.ToString}\n\n\n\n");
                        }
                    }
                    _db.AddRange(products);
                    _db.SaveChanges();
                    return new Result() { Status = true, Message = "" };
                }
                catch (Exception e)
                {
                    return new Result() { Status = false, Message = "مشکلی رخ داده است: در خواندن اطلاعات از گوگل شیت و درج ان در برنامه مشکلی پیش آمده:\n" + e.ToString() };
                }
            });
            return result;
        }

        
    }
}
