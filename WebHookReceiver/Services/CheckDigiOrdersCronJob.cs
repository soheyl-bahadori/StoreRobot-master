using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using StoreRobot.Utils;
using WebHookReceiver.Models;
using Microsoft.EntityFrameworkCore;
using System;
using WebHookReceiver.Controllers;

namespace WebHookReceiver.Services
{
    public class CheckDigiOrdersCronJob : CronJobService
    {
        private readonly ILogger<CheckDigiOrdersCronJob> _logger;

        public CheckDigiOrdersCronJob(IScheduleConfig<CheckDigiOrdersCronJob> config, ILogger<CheckDigiOrdersCronJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            File.AppendAllText("CronJobLogs.txt", $"CronJob 1 starts.\n.\n.\n");

            return base.StartAsync(cancellationToken);
        }

        public async override Task<Task> DoWork(CancellationToken cancellationToken)
        {
            try
            {
                //create new instance of db context
                var connectionString = "Data source=WebHook.db";
                var optionsBuilder = new DbContextOptionsBuilder<WebHookDbContext>();
                optionsBuilder.UseSqlite(connectionString);
                WebHookDbContext db = new(optionsBuilder.Options);
                File.AppendAllText("CronJobLogs.txt", $"{DateTime.Now:T} CronJob 1 is working.\n.\n.\n");
                var main = new MainController();

                var digiApi = new DigiKalaApi();
                var lastPage = Convert.ToInt32(digiApi.GetOrdersPage().Message);
                var orders1 = (JArray)JsonConvert.DeserializeObject(digiApi.GetOrders(lastPage).Message);
                var orders2 = (JArray)JsonConvert.DeserializeObject(digiApi.GetOrders(lastPage - 1).Message);
                JArray orders = new(orders1.Concat(orders2));

                foreach (var order in orders)
                {
                    if ((string)order["order_status"] == "warehouse" && (string)order["shipment_status"] == "pending" &&
                                !db.Orders.Any(o => o.OrderId == (string)order["order_item_id"] && o.Store == StoreEnum.DigiKala))
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
                        db.Orders.Add(new Order()
                        {
                            OrderId = (string)order["order_item_id"],
                            Store = StoreEnum.DigiKala,
                            Dkpc = (string)order["variant"]["id"],
                            Quantity = finalQuantity
                        });
                        db.SaveChanges();
                        if (finalQuantity != 0)
                        {
                            var jsons = await sheetApi.EditStockQuantityCell((string)order["variant"]["id"], -1 * finalQuantity, true);
                            foreach (var json in jsons)
                            {
                                var jsonObj = (JObject)JsonConvert.DeserializeObject(json);
                                await main.GoogleSheetsEdited(json);
                                var childrenJsons = await sheetApi.GetChildrenProducts((string)jsonObj["sku"]);
                                foreach (var childJson in childrenJsons)
                                {
                                    Thread.Sleep(2000);
                                    await main.GoogleSheetsEdited(childJson);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Directory.CreateDirectory("CronJobErrors");
                var random = new Random();
                System.IO.File.AppendAllText($"CronJobErrors/Error---{random.Next(100000, 999999)}.txt", e.ToString());
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            File.AppendAllText("CronJobLogs.txt", $"CronJob 1 is stopping.\n.\n.\n");

            return base.StopAsync(cancellationToken);
        }
    }
}
