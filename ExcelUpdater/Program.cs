using ExcelUpdater.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Sheets.v4.Data;

try
{
    Console.WriteLine("Robot is working...");
    string directoryPath = Environment.GetFolderPath(
                            System.Environment.SpecialFolder.DesktopDirectory) + "\\setup folder";
    if (!Directory.Exists(directoryPath))
    {
        Directory.CreateDirectory(directoryPath);
    }
    string searchPattern = "*.xlsx";

    string[] xlsxFiles = Directory.GetFiles(directoryPath, searchPattern);
    if (xlsxFiles.Length == 0)
    {
        throw new Exception("There is no xlsx file in '/Desktop/setup folder'; please add a file!");
    }
    string xlsxFile = xlsxFiles[0];
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    using (var package = new ExcelPackage(new FileInfo(xlsxFile)))
    {
        ExcelWorksheet workSheet = package.Workbook.Worksheets[0]; // Assuming you're reading the first worksheet

        //find column numbers
        var headerRow = workSheet.Cells[1, 1, 1, workSheet.Dimension.End.Column];

        // Iterate through the header cells to find the "stock" column
        int stockColumnIndex = -1;
        int variantColumnIndex = -1;
        foreach (var cell in headerRow)
        {
            if (cell.Text.Trim().Equals("تعداد ارسالی", StringComparison.OrdinalIgnoreCase))
            {
                stockColumnIndex = cell.Start.Column;
            }
            else if (cell.Text.Trim().Equals("کد تنوع", StringComparison.OrdinalIgnoreCase))
            {
                variantColumnIndex = cell.Start.Column;
            }
            if (stockColumnIndex != -1 && variantColumnIndex != -1)
                break;
        }
        if (stockColumnIndex == -1 || variantColumnIndex == -1)
            throw new Exception("The Exel is not in a correct format!");

        int rowCount = workSheet.Dimension.Rows;
        for (int row = 2; row <= rowCount; row++)
        {
            int quantity = Convert.ToInt32(workSheet.Cells[row, stockColumnIndex].Text);
            string dkpc = workSheet.Cells[row, variantColumnIndex].Text;

            Console.Write($"Updating {dkpc}...    ");

            var sheetApi = new GoogleSheetApi();

            var jsons = await sheetApi.EditStockQuantityCell(dkpc, -1 * quantity);
            foreach (var json in jsons)
            {
                var jsonObj = (JObject)JsonConvert.DeserializeObject(json);
                var childrenJsons = await sheetApi.GetChildrenProducts((string)jsonObj["sku"]);
                await GoogleSheetsEdited(json);
                foreach (var childJson in childrenJsons)
                {
                    await GoogleSheetsEdited(childJson);
                }
            }

            Console.WriteLine("Completed!\n");
        }
    }


    Console.WriteLine("\n\n\nAll products updated.");
    Console.ReadKey();
}
catch (Exception e)
{
    Console.WriteLine("\n\n\nAn error occurred: " + e.Message);
}

Console.WriteLine("\n\nPress any key to exit...");
Console.ReadKey();

async Task GoogleSheetsEdited(string internalBody)
{
    ///////////////////logging//////////////////

    //get request body
    string inputString = internalBody;

    JObject inputObj;
    try
    {
        inputObj = (JObject)JsonConvert.DeserializeObject(inputString);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error occurred:\n{e.ToString}\n\n\n\n\n\n\n\nContinuing, please wait...");
        return;
    }

    try
    {
        if (!string.IsNullOrEmpty(inputString))
        {
            Directory.CreateDirectory("BodiesJsonSheets");
            var random = new Random();
            var fileName = $"BodiesJsonSheets/{(string)inputObj["dkpc"]}---{random.Next(1000, 9999)}.txt";
            await System.IO.File.AppendAllTextAsync(fileName, inputString);
        }
        else
            return;

        ////////main work
        await Task.Run(async () =>
        {
            if ((int)inputObj["stock"] < 0)
                inputObj["stock"] = 0;
            if ((int)inputObj["digiStock"] < 0)
                inputObj["digiStock"] = 0;
            if ((int)inputObj["pakhshAndSafirStock"] < 0)
                inputObj["pakhshAndSafirStock"] = 0;
            if (string.IsNullOrEmpty((string)inputObj["guarantee"]))
                inputObj["guarantee"] = "اصالت و سلامت فیزیکی کالا";

            if ((bool)inputObj["isSafirChanged"])
            {
                //safir kala
                try
                {
                    var safirApi = new WooCommerceApi(StoreEnum.SafirKala);
                    var productString = await safirApi.GetProductAsync(new List<string>() { (string)inputObj["sku"] });
                    if (!string.IsNullOrEmpty(productString.Replace("[", "").Replace("]", "")))
                    {
                        var productObj = (JObject)((JArray)JsonConvert.DeserializeObject(productString)).FirstOrDefault();
                        var id = (string)productObj["id"];
                        string? parentId = null;
                        if ((string)productObj["type"] == "variation")
                            parentId = (string)productObj["parent_id"];

                        //update parent attributes
                        if (parentId != null)
                        {
                            var parentString = await safirApi.GetProductAsync(parentId);
                            if (!string.IsNullOrEmpty(parentString.Replace("[", "").Replace("]", "")))
                            {
                                var parentObj = JsonConvert.DeserializeObject(parentString);
                                var parentAttributes = Utilities.ChangeAttributes(JsonConvert.SerializeObject(parentObj), (string)inputObj["expiryDate"],
                                                        (string)inputObj["guarantee"], StoreEnum.SafirKala, false, true);
                                await safirApi.UpdateProductAsync(parentId, 0, "", true, "", parentAttributes, true, null);
                            }
                        }

                        //update whole api
                        var attributes = Utilities.ChangeAttributes(JsonConvert.SerializeObject(productObj), (string)inputObj["expiryDate"],
                                            (string)inputObj["guarantee"], StoreEnum.SafirKala, parentId != null, false);
                        await safirApi.UpdateProductAsync(id, (int)Convert.ToDouble(inputObj["pakhshAndSafirStock"]), (string)inputObj["price"],
                            (bool)inputObj["isSafirOff"], (string)inputObj["safirPriceInOff"], attributes, false, parentId);
                    }
                }
                catch (Exception e)
                {
                    Directory.CreateDirectory("BodiesJsonSheets");
                    var random = new Random();
                    await System.IO.File.AppendAllTextAsync($"BodiesJsonSheets/SafirError{(string)inputObj["dkpc"]}---{random.Next(1000, 9999)}.txt", e.ToString());
                }
            }


            if ((bool)inputObj["isPakhshChanged"])
            {
                //pakhsh
                try
                {
                    var pakhshApi = new WooCommerceApi(StoreEnum.Pakhsh);
                    var productString = await pakhshApi.GetProductAsync(new List<string>() { (string)inputObj["sku"] });
                    if (!string.IsNullOrEmpty(productString.Replace("[", "").Replace("]", "")))
                    {
                        var productObj = (JObject)((JArray)JsonConvert.DeserializeObject(productString)).FirstOrDefault();
                        var id = (string)productObj["id"];

                        //find parent id
                        string? parentId = null;
                        if ((string)productObj["type"] == "variation")
                            parentId = (string)productObj["parent_id"];

                        //update parent attributes
                        if (parentId != null)
                        {
                            var parentString = await pakhshApi.GetProductAsync(parentId);
                            if (!string.IsNullOrEmpty(parentString.Replace("[", "").Replace("]", "")))
                            {
                                var parentObj = JsonConvert.DeserializeObject(parentString);
                                var parentAttributes = Utilities.ChangeAttributes(JsonConvert.SerializeObject(parentObj), (string)inputObj["expiryDate"],
                                                        (string)inputObj["guarantee"], StoreEnum.Pakhsh, false, true);
                                await pakhshApi.UpdateProductAsync(parentId, 0, "", true, "", parentAttributes, true, null);
                            }
                        }

                        //update whole product
                        var attributes = Utilities.ChangeAttributes(JsonConvert.SerializeObject(productObj), (string)inputObj["expiryDate"],
                                            (string)inputObj["guarantee"], StoreEnum.Pakhsh, parentId != null, false);
                        await pakhshApi.UpdateProductAsync(id, (int)Convert.ToDouble(inputObj["pakhshAndSafirStock"]), (string)inputObj["price"],
                            (bool)inputObj["isPakhshOff"], (string)inputObj["pakhshPriceInOff"], attributes, false, parentId);
                    }
                }
                catch (Exception e)
                {
                    Directory.CreateDirectory("BodiesJsonSheets");
                    var random = new Random();
                    await System.IO.File.AppendAllTextAsync($"BodiesJsonSheets/PakhshError{(string)inputObj["sku"]}---{random.Next(1000, 9999)}.txt", e.ToString());
                }
            }
        });
    }
    catch (Exception e)
    {
        Directory.CreateDirectory("BodiesJsonSheets");
        var random = new Random();
        await System.IO.File.AppendAllTextAsync($"BodiesJsonSheets/Error{(string)inputObj["dkpc"]}---{random.Next(1000, 9999)}.txt", e.ToString());
    }
}
