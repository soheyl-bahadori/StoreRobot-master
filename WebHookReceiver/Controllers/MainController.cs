using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoreRobot.Utils;
using WebHookReceiver.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace WebHookReceiver.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    {

        [Route("SafirOrderEdited")]
        [HttpPost]
        public async Task<IActionResult> SafirOrderEdited()
        {

            //get request body
            string order = await new StreamReader(Request.Body).ReadToEndAsync();

            //deserialize to object
            JObject orderObj;
            try
            {
                orderObj = (JObject)JsonConvert.DeserializeObject(order);
            }
            catch
            {
                return Ok();
            }

            if (orderObj == null)
                return Ok();

            //get needed values
            var status = (string)orderObj["status"];
            var orderId = (string)orderObj["id"];
            var products = (JArray)orderObj["line_items"];

            try
            {
                Directory.CreateDirectory("BodiesJsonSafir");
                var random = new Random();
                await System.IO.File.AppendAllTextAsync($"BodiesJsonSafir/{orderId}---{random.Next(1000, 9999)}.txt", order);
            }
            catch (Exception)
            {
                // ignored
            }

#pragma warning disable CS4014
            Task.Run(async () =>
            {
                //create new instance of db context
                var connectionString = "Data source=WebHook.db";
                var optionsBuilder = new DbContextOptionsBuilder<WebHookDbContext>();
                optionsBuilder.UseSqlite(connectionString);
                WebHookDbContext db = new(optionsBuilder.Options);

                try
                {
                    // Define google sheet api
                    GoogleSheetApi sheetApi;

                    if (status is "processing" or "on-hold" &&
                        !db.Orders.Any(o => o.OrderId == orderId && o.Store == StoreEnum.SafirKala))
                    {
                        sheetApi = new GoogleSheetApi(StoreEnum.SafirKala);
                        await db.Orders.AddAsync(new Order()
                        {
                            OrderId = orderId,
                            Store = StoreEnum.SafirKala
                        });
                        await db.SaveChangesAsync();
                        foreach (var product in products)
                        {
                            var jsons = await sheetApi.EditStockQuantityCell((string)product["sku"], -1 * (int)product["quantity"], false);
                            foreach (var json in jsons)
                            {
                                var jsonObj = (JObject)JsonConvert.DeserializeObject(json);
                                var childrenJsons = await sheetApi.GetChildrenProducts((string)jsonObj["sku"]);
                                await GoogleSheetsEdited(json);
                                foreach (var childJson in childrenJsons)
                                {
                                    Thread.Sleep(2000);
                                    await GoogleSheetsEdited(childJson);
                                }
                            }
                        }
                    }
                    else if (status is "cancelled" or "refunded" or "failed" or "cancel-request" &&
                             db.Orders.Any(o => o.OrderId == orderId && o.Store == StoreEnum.SafirKala))
                    {
                        sheetApi = new GoogleSheetApi(StoreEnum.SafirKala);
                        foreach (var product in products)
                        {
                            var jsons = await sheetApi.EditStockQuantityCell((string)product["sku"], (int)product["quantity"], false);
                            foreach (var json in jsons)
                            {
                                var jsonObj = (JObject)JsonConvert.DeserializeObject(json);
                                var childrenJsons = await sheetApi.GetChildrenProducts((string)jsonObj["sku"]);
                                await GoogleSheetsEdited(json);
                                foreach (var childJson in childrenJsons)
                                {
                                    Thread.Sleep(2000);
                                    await GoogleSheetsEdited(childJson);
                                }
                            }
                        }
                        db.Orders.Remove(db.Orders.FirstOrDefault(o => o.OrderId == orderId && o.Store == StoreEnum.SafirKala));
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        Directory.CreateDirectory("BodiesJsonSafir");
                        var random = new Random();
                        await System.IO.File.AppendAllTextAsync($"BodiesJsonSafir/Error{orderId}---{random.Next(1000, 9999)}.txt", e.ToString());
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            });

            return Ok();
        }

        [Route("PakhshOrderEdited")]
        [HttpPost]
        public async Task<IActionResult> PakhshOrderEdited()
        {

            //get request body
            string order = await new StreamReader(Request.Body).ReadToEndAsync();

            //deserialize to object
            JObject orderObj;
            try
            {
                orderObj = (JObject)JsonConvert.DeserializeObject(order);
            }
            catch
            {
                return Ok();
            }

            if (orderObj == null)
                return Ok();

            //get needed values
            var status = (string)orderObj["status"];
            var orderId = (string)orderObj["id"];
            var products = (JArray)orderObj["line_items"];

            try
            {
                Directory.CreateDirectory("BodiesJsonPakhsh");
                var random = new Random();
                await System.IO.File.AppendAllTextAsync($"BodiesJsonPakhsh/{orderId}---{random.Next(1000, 9999)}.txt", order);
            }
            catch (Exception)
            {
                // ignored
            }
#pragma warning disable CS4014
            Task.Run(async () =>
            {
                //create new instance of db context
                var connectionString = "Data source=WebHook.db";
                var optionsBuilder = new DbContextOptionsBuilder<WebHookDbContext>();
                optionsBuilder.UseSqlite(connectionString);
                WebHookDbContext db = new(optionsBuilder.Options);

                try
                {
                    // Define google sheet api
                    GoogleSheetApi sheetApi;

                    if (status is "processing" or "on-hold" &&
                        !db.Orders.Any(o => o.OrderId == orderId && o.Store == StoreEnum.Pakhsh))
                    {
                        sheetApi = new GoogleSheetApi(StoreEnum.Pakhsh);
                        await db.Orders.AddAsync(new Order()
                        {
                            OrderId = orderId,
                            Store = StoreEnum.Pakhsh
                        });
                        await db.SaveChangesAsync();
                        foreach (var product in products)
                        {
                            var jsons = await sheetApi.EditStockQuantityCell((string)product["sku"], -1 * (int)product["quantity"], false);
                            foreach (var json in jsons)
                            {
                                var jsonObj = (JObject)JsonConvert.DeserializeObject(json);
                                var childrenJsons = await sheetApi.GetChildrenProducts((string)jsonObj["sku"]);
                                await GoogleSheetsEdited(json);
                                foreach (var childJson in childrenJsons)
                                {
                                    Thread.Sleep(2000);
                                    await GoogleSheetsEdited(childJson);
                                }
                            }
                        }
                    }
                    else if (status is "cancelled" or "refunded" or "failed" or "cancel-request" &&
                             db.Orders.Any(o => o.OrderId == orderId && o.Store == StoreEnum.Pakhsh))
                    {
                        sheetApi = new GoogleSheetApi(StoreEnum.Pakhsh);
                        db.Orders.Remove(db.Orders.FirstOrDefault(o => o.OrderId == orderId && o.Store == StoreEnum.Pakhsh));
                        await db.SaveChangesAsync();
                        foreach (var product in products)
                        {
                            var jsons = await sheetApi.EditStockQuantityCell((string)product["sku"], (int)product["quantity"], false);
                            foreach (var json in jsons)
                            {
                                var jsonObj = (JObject)JsonConvert.DeserializeObject(json);
                                var childrenJsons = await sheetApi.GetChildrenProducts((string)jsonObj["sku"]);
                                await GoogleSheetsEdited(json);
                                foreach (var childJson in childrenJsons)
                                {
                                    Thread.Sleep(2000);
                                    await GoogleSheetsEdited(childJson);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        Directory.CreateDirectory("BodiesJsonPakhsh");
                        var random = new Random();
                        await System.IO.File.AppendAllTextAsync($"BodiesJsonPakhsh/Error{orderId}---{random.Next(1000, 9999)}.txt", e.ToString());
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            });

            return Ok();
        }


        [Route("DigiOrderEdited")]
        [HttpPost]
        public async Task<IActionResult> DigiOrderEdited()
        {
            ///////////////////logging//////////////////

            Console.WriteLine("Request received!");
            await Console.Out.FlushAsync();

            //get request body
            string orderString = await new StreamReader(Request.Body).ReadToEndAsync();

            JArray orderObj;
            try
            {
                orderObj = (JArray)JsonConvert.DeserializeObject(orderString);
            }
            catch
            {
                Console.WriteLine("Error occurred!\n");
                await Console.Out.FlushAsync();
                return Ok();
            }

            try
            {
                string authHeader = Request.Headers["Authorization"];
                Console.WriteLine("Auth code: " + authHeader);
                await Console.Out.FlushAsync();

                if (!string.IsNullOrEmpty(orderString))
                {
                    Console.WriteLine($"Order id: {(string)orderObj[0]["order_id"]}\nOrder status: {(string)orderObj[0]["order_status"]}\nShipment status: {(string)orderObj[0]["shipment_status"]}");
                    await Console.Out.FlushAsync();

                    Directory.CreateDirectory("BodiesJson");
                    var random = new Random();
                    await System.IO.File.AppendAllTextAsync($"BodiesJson/{(string)orderObj[0]["order_id"]}---{random.Next(1000, 9999)}.txt", orderString);
                }
                else
                {
                    Console.WriteLine(DateTime.Now.ToString("g") + "\n.\n.\n.\n.\n.\n.\n.\n");
                    await Console.Out.FlushAsync();
                    return Ok();
                }
                Console.WriteLine(DateTime.Now.ToString("g") + "\n.\n.\n.\n.\n.\n.\n.\n");
                await Console.Out.FlushAsync();

                ////////main work
#pragma warning disable CS4014
                Task.Run(async () =>
                {
                    //create new instance of db context
                    var connectionString = "Data source=WebHook.db";
                    var optionsBuilder = new DbContextOptionsBuilder<WebHookDbContext>();
                    optionsBuilder.UseSqlite(connectionString);
                    WebHookDbContext db = new(optionsBuilder.Options);


                    try
                    {
                        foreach (var order in orderObj)
                        {
                            if ((string)order["order_status"] == "warehouse" && (string)order["shipment_status"] == "pending" &&
                                !db.Orders.Any(o => o.OrderId == (string)order["order_item_id"] && o.Store == StoreEnum.DigiKala) /*&&
                                DateTime.Compare((DateTime)order["created_at"], DateTime.Now.AddMinutes(-60)) > 0*/)
                            {
                                //Count quantity
                                var oldReservedStock = ((int)order["variant"]["stock"]["reserved_stocks"]["digikala"]) -
                                                       ((int)order["quantity"]);
                                if (oldReservedStock < 0) oldReservedStock = (int)order["variant"]["stock"]["reserved_stocks"]["digikala"];
                                var digikalaQuantity = ((int)order["variant"]["stock"]["in_digikala_warehouse"]) -
                                                       oldReservedStock;
                                if (digikalaQuantity < 0) digikalaQuantity = 0;
                                var finalQuantity = 0;
                                if (((string)order["shipping_type"]).Contains("seller"))
                                {
                                    finalQuantity = (int)order["quantity"];
                                }
                                else if ((int)order["quantity"] > digikalaQuantity)
                                {
                                    finalQuantity = (int)order["quantity"] - digikalaQuantity;
                                }

                                var sheetApi = new GoogleSheetApi(StoreEnum.DigiKala);
                                await db.Orders.AddAsync(new Order()
                                {
                                    OrderId = (string)order["order_item_id"],
                                    Store = StoreEnum.DigiKala,
                                    Dkpc = (string)order["variant"]["id"],
                                    Quantity = finalQuantity
                                });
                                await db.SaveChangesAsync();
                                if (finalQuantity != 0)
                                {
                                    var jsons = await sheetApi.EditStockQuantityCell((string)order["variant"]["id"], -1 * finalQuantity, true);
                                    foreach (var json in jsons)
                                    {
                                        var jsonObj = (JObject)JsonConvert.DeserializeObject(json);
                                        var childrenJsons = await sheetApi.GetChildrenProducts((string)jsonObj["sku"]);
                                        await GoogleSheetsEdited(json);
                                        foreach (var childJson in childrenJsons)
                                        {
                                            Thread.Sleep(2000);
                                            await GoogleSheetsEdited(childJson);
                                        }
                                    }
                                }
                            }
                            //else if ((string)order["order_status"] == "warehouse_processing" && (string)order["shipment_status"] == "processing" &&
                            //         db.Orders.Any(o => o.OrderId == (string)order["order_item_id"] && o.Store == Store.DigiKala))
                            //{
                            //    db.Orders.Remove(db.Orders.FirstOrDefault(o => o.OrderId == (string)order["order_item_id"] && o.Store == Store.DigiKala));
                            //    await db.SaveChangesAsync();
                            //}
                        }
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            Directory.CreateDirectory("BodiesJson");
                            var random = new Random();
                            await System.IO.File.AppendAllTextAsync($"BodiesJson/Error{(string)orderObj[0]["order_id"]}---{random.Next(1000, 9999)}.txt", e.ToString());
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Directory.CreateDirectory("BodiesJson");
                var random = new Random();
                await System.IO.File.AppendAllTextAsync($"BodiesJson/Error{(string)orderObj[0]["order_id"]}---{random.Next(1000, 9999)}.txt", e.ToString());
            }



            return Ok();
        }


        [Route("GoogleSheetsEdited")]
        [HttpPost]
        public async Task<IActionResult> GoogleSheetsEdited(string? internalBody)
        {
            ///////////////////logging//////////////////

            Console.WriteLine("Request received!");
            await Console.Out.FlushAsync();
            bool isInternal = false;

            //get request body
            string? inputString = "";
            try
            {
                inputString = await new StreamReader(Request.Body).ReadToEndAsync();
            }
            catch
            {
                //ignored
            }
            if (string.IsNullOrEmpty(inputString))
            {
                inputString = internalBody;
                isInternal = true;
            }

            JObject inputObj;
            try
            {
                inputObj = (JObject)JsonConvert.DeserializeObject(inputString);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error occurred!\n{e.ToString}");
                await Console.Out.FlushAsync();
                return Ok();
            }

            try
            {
                if (!string.IsNullOrEmpty(inputString))
                {
                    Console.WriteLine($"Dkpc: {(string)inputObj["dkpc"]}\nSku: {(string)inputObj["sku"]}\nPrice: {(string)inputObj["price"]}\nStock: {(string)inputObj["stock"]}");
                    await Console.Out.FlushAsync();

                    Directory.CreateDirectory("BodiesJsonSheets");
                    var random = new Random();
                    var fileName = isInternal ? $"BodiesJsonSheets/{(string)inputObj["sku"]}---Internal---{random.Next(1000, 9999)}.txt" :
                        $"BodiesJsonSheets/{(string)inputObj["sku"]}---{random.Next(1000, 9999)}.txt";
                    await System.IO.File.AppendAllTextAsync(fileName, inputString);
                }
                else
                {
                    Console.WriteLine(DateTime.Now.ToString("g") + "\n.\n.\n.\n.\n.\n.\n.\n");
                    await Console.Out.FlushAsync();
                    return Ok();
                }
                Console.WriteLine(DateTime.Now.ToString("g") + "\n.\n.\n.\n.\n.\n.\n.\n");
                await Console.Out.FlushAsync();

                ////////main work
#pragma warning disable CS4014
                Task.Run(async () =>
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
                            await System.IO.File.AppendAllTextAsync($"BodiesJsonSheets/SafirError{(string)inputObj["sku"]}---{random.Next(1000, 9999)}.txt", e.ToString());
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


                    if ((bool)inputObj["isDigiChanged"])
                    {
                        //digikala
                        try
                        {
                            DigiKalaApi digiKalaApi = new();

                            int stock = (int)Convert.ToDouble(inputObj["digiStock"]);

                            //get product from digikala
                            if (string.IsNullOrEmpty((string)inputObj["dkpc"]))
                                throw new Exception("dkpc is empty for this product.");
                            string digiProductString = digiKalaApi.GetProductAsync((string)inputObj["dkpc"]).Result;
                            var firstDigiProduct = (JObject)JsonConvert.DeserializeObject(digiProductString);

                            if ((bool)firstDigiProduct["is_active"])
                            {
                                if ((int)firstDigiProduct["stock"]["dk_reserved_stock"] >
                                            (int)firstDigiProduct["stock"]["dk_stock"])
                                {
                                    stock = stock + (int)firstDigiProduct["stock"]["seller_reserved_stock"] +
                                                    (int)firstDigiProduct["stock"]["dk_reserved_stock"] -
                                                    (int)firstDigiProduct["stock"]["dk_stock"];
                                }
                                else
                                {
                                    stock = stock + (int)firstDigiProduct["stock"]["seller_reserved_stock"];
                                }

                                var updateApiResult = await digiKalaApi.UpdateProductAsync((string)inputObj["dkpc"], stock,
                                    true, (int)firstDigiProduct["stock"]["seller_stock"], true);
                                if (!updateApiResult.Status)
                                {
                                    throw new Exception(updateApiResult.Message);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Directory.CreateDirectory("BodiesJsonSheets");
                            var random = new Random();
                            await System.IO.File.AppendAllTextAsync($"BodiesJsonSheets/DigiError{(string)inputObj["sku"]}---{random.Next(1000, 9999)}.txt", e.ToString());
                        }
                    }
                });
            }
            catch (Exception e)
            {
                try
                {
                    Directory.CreateDirectory("BodiesJsonSheets");
                    var random = new Random();
                    await System.IO.File.AppendAllTextAsync($"BodiesJsonSheets/Error{(string)inputObj["sku"]}---{random.Next(1000, 9999)}.txt", e.ToString());
                }
                catch (Exception)
                {
                    // ignored
                }
            }



            return Ok();
        }
    }
}