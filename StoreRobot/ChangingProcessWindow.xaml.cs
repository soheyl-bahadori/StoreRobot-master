using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Shapes;
using StoreRobot.Models;
using StoreRobot.Utils;
using System.Text.RegularExpressions;

namespace StoreRobot
{
    public enum ProcessMode
    {
        Auto,
        Manual
    }
    /// <summary>
    /// Interaction logic for ChangeProcessWindow.xaml
    /// </summary>
    public partial class ChangingProcessWindow : Window
    {
        private bool _IsSafirSelected;
        private bool _IsPakhshSelected;
        private bool _IsDigiSelected;
        private string _DigiTotalMinutes;
        private string _SafirTotalMinutes;
        private string _PakhshTotalMinutes;
        private ProcessMode _processMode;
        private int _productIndex = 0;
        private ApplicationDbContext _db = new();
        CancellationTokenSource _source = new();

        public ChangingProcessWindow(bool isSafirSelected, bool isPakhshSelected, bool isDigiSelected, ProcessMode processMode)
        {
            _IsSafirSelected = isSafirSelected;
            _IsPakhshSelected = isPakhshSelected;
            _IsDigiSelected = isDigiSelected;
            _processMode = processMode;
            InitializeComponent();
        }

        #region TopRow
        private void Rectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            WarningMessage warningMessage = new();
            var dialogResult = warningMessage.ShowDialog();
            if (dialogResult.Value)
                Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {

            SafirGrid.IsEnabled = _IsSafirSelected;
            PakhshGrid.IsEnabled = _IsPakhshSelected;
            DigiKalaGrid.IsEnabled = _IsDigiSelected;


            CancellationToken token = _source.Token;
            await Task.Run(() =>
            {
                try
                {
                    //Update database from sheet
                    var googleSheet = new GoogleSheetApi(_db);
                    var result = googleSheet.AddSheetToDbAsync().Result;
                    if (!result.Status)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ErrorMessage error = new ErrorMessage(result.Message);
                            error.ShowDialog();
                            Close();
                        });
                        return;
                    }

                    //Remove all old errors
                    if (_db.ProductErrors.Any())
                        _db.ProductErrors.RemoveRange(_db.ProductErrors.ToList());
                    _db.SaveChanges();

                    List<Task<ProductErrors?>> productTasks = new();

                    //Update changes in Safir Kala
                    if (_IsSafirSelected)
                    {
                        var watch = new Stopwatch();
                        watch.Start();

                        //define api instances
                        WooCommerceApi safirApi = new WooCommerceApi(StoreEnum.SafirKala);

                        //Loop over products
                        int mainCount = _db.Products.Count() % 100 != 0
                            ? _db.Products.Count() / 100 + 1
                            : _db.Products.Count() / 100;
                        for (int i = 0; i < mainCount; i++)
                        {
                            // var watch = new Stopwatch();
                            // watch.Start();
                            //find changed products
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            var defaultProducts = _db.Products.Skip(i * 100).Take(100)
                                .Where(p => !string.IsNullOrEmpty(p.Name)).ToList();
                            var jsonProduct = safirApi.GetProductAsync(defaultProducts.Select(p => p.Sku).ToList()).Result;
                            if (jsonProduct == null)
                            {
                                continue;
                            }
                            JArray jsonResult = (JArray)JsonConvert.DeserializeObject(jsonProduct);
                            var modifiedProducts = (from productJson in jsonResult
                                                    let isSafirChanged =
                                                        Utilities.IsProductChanged(JsonConvert.SerializeObject(productJson),
                                                            defaultProducts.FirstOrDefault(p => p.Sku == (string)productJson["sku"]),
                                                            StoreEnum.SafirKala)
                                                    where isSafirChanged
                                                    select defaultProducts.FirstOrDefault(p => p.Sku == (string)productJson["sku"])).ToList();

                            //update products
                            int syncCount = 5;
                            int count = modifiedProducts.Count() % syncCount != 0
                                ? modifiedProducts.Count() / syncCount + 1
                                : modifiedProducts.Count() / syncCount;
                            for (int j = 0; j < count; j++)
                            {
                                foreach (var product in modifiedProducts.Skip(j * syncCount).Take(syncCount).ToList())
                                {
                                    if (token.IsCancellationRequested)
                                    {
                                        return;
                                    }

                                    productTasks.Add(Task.Run(
                                        () => UpdateProduct(product,
                                            safirApi,
                                            JsonConvert.SerializeObject(
                                                jsonResult.FirstOrDefault(js => (string)js["sku"] == product.Sku)),
                                            StoreEnum.SafirKala),
                                        token));
                                }

                                //finish
                                var productError = Task.WhenAll(productTasks).Result;
                                _db.ProductErrors.AddRange(productError.Where(p => p != null)!);
                                _db.SaveChanges();
                                productTasks = new();
                            }

                            var i1 = i;
                            Dispatcher.Invoke(() =>
                            {
                                int percent = ((i1 + 1) * 100 / mainCount);
                                SafirPercent.Text =
                                    $"({percent}%) ...";
                            });
                            // watch.Stop();
                            // var felan = watch.Elapsed.TotalMinutes;
                        }

                        watch.Stop();
                        _SafirTotalMinutes = watch.Elapsed.TotalMinutes.ToString();
                        Dispatcher.Invoke(() =>
                        {
                            SafirPercent.Text = "(تمام شد)";
                            SafirImage.Source = new BitmapImage(
                                new Uri("pack://application:,,,/Rahkar Mofid;component/Assets/Green Tick.png"));
                        });
                    }

                    //Update changes in pakhsh
                    if (_IsPakhshSelected)
                    {
                        var watch = new Stopwatch();
                        watch.Start();

                        //define api instances
                        WooCommerceApi pakhshApi = new WooCommerceApi(StoreEnum.Pakhsh);

                        //Loop over products
                        int mainCount = _db.Products.Count() % 100 != 0
                            ? _db.Products.Count() / 100 + 1
                            : _db.Products.Count() / 100;
                        for (int i = 0; i < mainCount; i++)
                        {
                            // var watch = new Stopwatch();
                            // watch.Start();
                            //find changed products
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            var defaultProducts = _db.Products.Skip(i * 100).Take(100)
                                .Where(p => !string.IsNullOrEmpty(p.Name)).ToList();
                            var jsonProduct = pakhshApi.GetProductAsync(defaultProducts.Select(p => p.Sku).ToList())
                                .Result;
                            if (jsonProduct == null)
                            {
                                continue;
                            }
                            JArray jsonResult = (JArray)JsonConvert.DeserializeObject(jsonProduct);
                            var modifiedProducts = (from productJson in jsonResult
                                                    let isPakhshChanged =
                                                        Utilities.IsProductChanged(JsonConvert.SerializeObject(productJson),
                                                            defaultProducts.FirstOrDefault(p => p.Sku == (string)productJson["sku"]),
                                                            StoreEnum.Pakhsh)
                                                    where isPakhshChanged
                                                    select defaultProducts.FirstOrDefault(p => p.Sku == (string)productJson["sku"]))
                                .ToList();

                            //update products
                            int syncCount = 5;
                            int count = modifiedProducts.Count() % syncCount != 0
                                ? modifiedProducts.Count() / syncCount + 1
                                : modifiedProducts.Count() / syncCount;
                            for (int j = 0; j < count; j++)
                            {
                                foreach (var product in modifiedProducts.Skip(j * syncCount).Take(syncCount).ToList())
                                {
                                    if (token.IsCancellationRequested)
                                    {
                                        return;
                                    }

                                    productTasks.Add(Task.Run(
                                        () => UpdateProduct(product,
                                            pakhshApi,
                                            JsonConvert.SerializeObject(
                                                jsonResult.FirstOrDefault(js => (string)js["sku"] == product.Sku)),
                                            StoreEnum.Pakhsh),
                                        token));
                                }

                                //finish
                                var productError = Task.WhenAll(productTasks).Result;
                                _db.ProductErrors.AddRange(productError.Where(p => p != null)!);
                                _db.SaveChanges();
                                productTasks = new();
                            }

                            var i1 = i;
                            Dispatcher.Invoke(() =>
                            {
                                int percent = ((i1 + 1) * 100 / mainCount);
                                PakhshPercent.Text =
                                    $"({percent}%) ...";
                            });
                            // watch.Stop();
                            // var felan = watch.Elapsed.TotalMinutes;
                        }

                        watch.Stop();
                        _PakhshTotalMinutes = watch.Elapsed.TotalMinutes.ToString();
                        Dispatcher.Invoke(() =>
                        {
                            PakhshPercent.Text = "(تمام شد)";
                            PakhshImage.Source = new BitmapImage(
                                new Uri("pack://application:,,,/Rahkar Mofid;component/Assets/Green Tick.png"));
                        });

                    }

                    //Update changes in digikala
                    if (_IsDigiSelected)
                    {

                        var watch = new Stopwatch();
                        watch.Start();
                        //define api instances
                        DigiKalaApi digiKalaApi = new();

                        //find commissions
                        /*var seleniumUtils = new SeleniumUtils();
                         var commissionUpdateResult = seleniumUtils.UpdateCommissions(_db).Result;
                         seleniumUtils.Exit();
                         if (!commissionUpdateResult.Result)
                         {
                             Dispatcher.Invoke(() =>
                             {
                                 var errorMessage = new ErrorMessage(commissionUpdateResult.Message);
                                 errorMessage.ShowDialog();
                                 Close();
                             });
                             _source.Cancel();
                             return;
                         }*/

                        var updateComisionStatus = digiKalaApi.UpdateCommisionAsync().Result;
                        if (!updateComisionStatus.Result)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ErrorMessage error = new ErrorMessage(updateComisionStatus.Message);
                                error.ShowDialog();
                                Close();
                            });
                            return;
                        }

                        //Loop over products
                        int syncCount = 2;
                        int mainCount = _db.Products.Count() % syncCount != 0
                            ? _db.Products.Count() / syncCount + 1
                            : _db.Products.Count() / syncCount;
                        for (int i = 0; i < mainCount; i++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            var products = _db.Products.Skip(i * syncCount).Take(syncCount)
                                .Where(p => !string.IsNullOrEmpty(p.Name)).ToList();
                            var productChangeTasks = new List<Task>();
                            foreach (var product in products)
                            {
                                if (token.IsCancellationRequested)
                                    return;
                                if (string.IsNullOrEmpty(product.Dkpc) ||
                                    product.Dkpc == "0" ||
                                    product.Dkpc.Contains('R') || product.DigiStatus == 0)
                                    continue;

                                //test
                                //Dispatcher.Invoke(() =>
                                //{
                                //    PakhshPercent.Text =
                                //        $"{product.Dkpc}";
                                //});

                                var stock = product.DigiStockQuantity;

                                //get product from digikala
                                string? digiProductString = digiKalaApi.GetProductAsync(product.Dkpc).Result;
                                if (string.IsNullOrEmpty(digiProductString))
                                {
                                    _db.ProductErrors.Add(new ProductErrors()
                                    {
                                        ErrorMessage =
                                            $"در خواندن اطلاعات کالای dkpc-{product.Dkpc} مشکلی به وجود آمده.",
                                        Store = StoreEnum.DigiKala,
                                        Name = product.Name,
                                        SKU = product.Dkpc,
                                        ReportNumber = 1
                                    });
                                    _db.SaveChanges();
                                    continue;
                                }
                                var firstDigiProduct = (JObject)JsonConvert.DeserializeObject(digiProductString);

                                //min price and old price
                                int oldPrice = (int)firstDigiProduct["price"]["selling_price"] / 10;
                                if ((bool)firstDigiProduct["extra"]["promotion_data"]["is_in_promotion"] &&
                                    !string.IsNullOrEmpty((string)firstDigiProduct["extra"]["promotion_data"]["promo_price"]))
                                {
                                    oldPrice = (int)firstDigiProduct["extra"]["promotion_data"]["promo_price"] / 10;
                                }
                                var commission = _db.Commissions.FirstOrDefault(c =>
                                    c.CategoryId == (int)firstDigiProduct["product"]["category_id"]);
                                if (commission == null)
                                {
                                    var db = new ApplicationDbContext();
                                    db.ProductErrors.Add(new ProductErrors()
                                    {
                                        ErrorMessage =
                                            $"کمیسیون این کالا پیدا نشد.",
                                        Store = StoreEnum.DigiKala,
                                        Name = product.Name,
                                        SKU = product.Dkpc
                                    });
                                    db.SaveChanges();
                                    continue;
                                }
                                int minPrice = DigiUtils.FindMinPrice(product.DomesticPrice, commission.CommissionPercent, product.CommisionMin, product.CommisionMax).ZeroConvert(1);
                                double digiPercent = (100 - DigiUtils.GetDigiPercent(product.DigiStatus)) / 100;


                                if (product.DigiStatus == 1 ||
                                    stock == 0 && (int)firstDigiProduct["stock"]["dk_reserved_stock"] >=
                                    (int)firstDigiProduct["stock"]["dk_stock"])
                                {
                                    productChangeTasks.Add(Task.Run(async () =>
                                    {
                                        //change product stock
                                        if ((int)firstDigiProduct["stock"]["dk_reserved_stock"] >
                                        (int)firstDigiProduct["stock"]["dk_stock"])
                                        {
                                            stock = stock +
                                                                    (int)firstDigiProduct["stock"]["seller_reserved_stock"] +
                                                                    (int)firstDigiProduct["stock"]["dk_reserved_stock"] -
                                                                    (int)firstDigiProduct["stock"]["dk_stock"];
                                        }
                                        else
                                        {
                                            stock = stock +
                                                                    (int)firstDigiProduct["stock"]["seller_reserved_stock"];
                                        }

                                        var updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                            product.DigiStatus == 2, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                        if (!updateApiResult.Status)
                                        {
                                            if (oldPrice < minPrice)
                                            {
                                                await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                    false, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                            }
                                            var db = new ApplicationDbContext();
                                            db.ProductErrors.Add(new ProductErrors()
                                            {
                                                ErrorMessage =
                                                    $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                Store = StoreEnum.DigiKala,
                                                Name = product.Name,
                                                SKU = product.Dkpc,
                                                Stock = stock.ToString(),
                                                UpdateError = updateApiResult.Message,
                                                ReportNumber = 2
                                            });
                                            db.SaveChanges();
                                        }
                                    }));
                                }
                                else if (product.DigiStatus == 2)
                                {
                                    productChangeTasks.Add(Task.Run(async () =>
                                    {
                                        //change product stock
                                        if ((int)firstDigiProduct["stock"]["dk_reserved_stock"] >
                                        (int)firstDigiProduct["stock"]["dk_stock"])
                                        {
                                            stock = stock +
                                                                    (int)firstDigiProduct["stock"]["seller_reserved_stock"] +
                                                                    (int)firstDigiProduct["stock"]["dk_reserved_stock"] -
                                                                    (int)firstDigiProduct["stock"]["dk_stock"];
                                        }
                                        else
                                        {
                                            stock = stock +
                                                                    (int)firstDigiProduct["stock"]["seller_reserved_stock"];
                                        }

                                        //db context
                                        var db = new ApplicationDbContext();


                                        //conditions
                                        if (oldPrice >= minPrice)
                                        {
                                            //update stock and is active
                                            var updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                            if (!updateApiResult.Status)
                                            {
                                                db.ProductErrors.Add(new ProductErrors()
                                                {
                                                    ErrorMessage =
                                                        $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                    Store = StoreEnum.DigiKala,
                                                    Name = product.Name,
                                                    SKU = product.Dkpc,
                                                    Stock = stock.ToString(),
                                                    UpdateError = updateApiResult.Message,
                                                    ReportNumber = 3
                                                });
                                                db.SaveChanges();
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            //update stock and price to min price
                                            var updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"],
                                                minPrice * 10);
                                            if (!updateApiResult.Status)
                                            {
                                                updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                    false, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                                db.ProductErrors.Add(new ProductErrors()
                                                {
                                                    ErrorMessage =
                                                        $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                    Store = StoreEnum.DigiKala,
                                                    Name = product.Name,
                                                    SKU = product.Dkpc,
                                                    Stock = stock.ToString(),
                                                    UpdateError = updateApiResult.Message,
                                                    ReportNumber = 4
                                                });
                                                db.SaveChanges();
                                                return;
                                            }
                                        }



                                    }));
                                }
                                else if (product.DigiStatus == 3)
                                {
                                    productChangeTasks.Add(Task.Run(async () =>
                                    {
                                        //change product stock
                                        if ((int)firstDigiProduct["stock"]["dk_reserved_stock"] >
                                        (int)firstDigiProduct["stock"]["dk_stock"])
                                        {
                                            stock = stock +
                                                                    (int)firstDigiProduct["stock"]["seller_reserved_stock"] +
                                                                    (int)firstDigiProduct["stock"]["dk_reserved_stock"] -
                                                                    (int)firstDigiProduct["stock"]["dk_stock"];
                                        }
                                        else
                                        {
                                            stock += (int)firstDigiProduct["stock"]["seller_reserved_stock"];
                                        }

                                        //db context
                                        var db = new ApplicationDbContext();

                                        //update stock
                                        var updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                            true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                        if (!updateApiResult.Status)
                                        {
                                            updateApiResult = DigiUtils.NormalPriceChanging(product, digiKalaApi, product.UserPrice,
                                                minPrice, product.JumpStep, (bool)firstDigiProduct["is_active"], token);
                                            if (!updateApiResult.Status)
                                            {
                                                if (oldPrice < minPrice)
                                                {
                                                    await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                        false, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                                }
                                                db.ProductErrors.Add(new ProductErrors()
                                                {
                                                    ErrorMessage =
                                                        $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                    Store = StoreEnum.DigiKala,
                                                    Name = product.Name,
                                                    SKU = product.Dkpc,
                                                    Stock = stock.ToString(),
                                                    UpdateError = updateApiResult.Message,
                                                    ReportNumber = 5
                                                });
                                                db.SaveChanges();
                                                return;
                                            }
                                        }


                                        //conditions
                                        if (oldPrice >= minPrice)
                                        {
                                            var price =
                                                (int?)firstDigiProduct["extra"]["buy_box"]["buy_box_price"] / 10;

                                            if (price == null)
                                            {
                                                updateApiResult = DigiUtils.NormalPriceChanging(product, digiKalaApi, product.UserPrice,
                                                    minPrice, product.JumpStep, (bool)firstDigiProduct["is_active"], token);
                                                if (!updateApiResult.Status)
                                                {
                                                    db.ProductErrors.Add(new ProductErrors()
                                                    {
                                                        ErrorMessage =
                                                            $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                        Store = StoreEnum.DigiKala,
                                                        Name = product.Name,
                                                        SKU = product.Dkpc,
                                                        Stock = stock.ToString(),
                                                        UpdateError = updateApiResult.Message,
                                                        ReportNumber = 5
                                                    });
                                                    db.SaveChanges();
                                                }
                                                return;
                                            }
                                            if (!(bool)firstDigiProduct["extra"]["buy_box"]["is_buy_box_winner"] &&
                                                !(bool)firstDigiProduct["extra"]["buy_box"]["is_seller_buy_box_winner"] &&
                                                price - product.JumpStep >= minPrice)
                                            {

                                                //update stock and is active
                                                updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                    true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"],
                                                    (price - product.JumpStep) * 10);
                                                if (!updateApiResult.Status)
                                                {
                                                    db.ProductErrors.Add(new ProductErrors()
                                                    {
                                                        ErrorMessage =
                                                            $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                        Store = StoreEnum.DigiKala,
                                                        Name = product.Name,
                                                        SKU = product.Dkpc,
                                                        Stock = stock.ToString(),
                                                        UpdateError = updateApiResult.Message,
                                                        ReportNumber = 6
                                                    });
                                                    db.SaveChanges();
                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                //db.ProductErrors.Add(new ProductErrors()
                                                //{
                                                //    ErrorMessage =
                                                //        $"کالای dkpc-{product.Dkpc} با موجودی {stock} برنده بای باکس بود یا قیمت بای باکس از حداقل قیمت کمتر بود و قیمت آن بدون تغییر ماند",
                                                //    Store = StoreEnum.DigiKala,
                                                //    Name = product.Name,
                                                //    SKU = product.Dkpc,
                                                //    Stock = stock.ToString(),
                                                //    UpdateError = "",
                                                //    ReportNumber = 20
                                                //});
                                                //db.SaveChanges();
                                            }
                                        }
                                        else
                                        {
                                            //update stock and price to min price
                                            updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"],
                                                minPrice * 10);
                                            if (!updateApiResult.Status)
                                            {
                                                updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                    false, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                                if (!updateApiResult.Status)
                                                {
                                                    db.ProductErrors.Add(new ProductErrors()
                                                    {
                                                        ErrorMessage =
                                                            $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                        Store = StoreEnum.DigiKala,
                                                        Name = product.Name,
                                                        SKU = product.Dkpc,
                                                        Stock = stock.ToString(),
                                                        UpdateError = updateApiResult.Message,
                                                        ReportNumber = 7
                                                    });
                                                    db.SaveChanges();
                                                    return;
                                                }
                                            }
                                        }



                                    }));
                                }
                                else if (product.DigiStatus >= 4 && product.DigiStatus != 5)
                                {
                                    productChangeTasks.Add(Task.Run(async () =>
                                    {
                                        //change product stock
                                        if ((int)firstDigiProduct["stock"]["dk_reserved_stock"] >
                                        (int)firstDigiProduct["stock"]["dk_stock"])
                                        {
                                            stock = stock +
                                                                    (int)firstDigiProduct["stock"]["seller_reserved_stock"] +
                                                                    (int)firstDigiProduct["stock"]["dk_reserved_stock"] -
                                                                    (int)firstDigiProduct["stock"]["dk_stock"];
                                        }
                                        else
                                        {
                                            stock += (int)firstDigiProduct["stock"]["seller_reserved_stock"];
                                        }

                                        //db context
                                        var db = new ApplicationDbContext();

                                        //min price and old price
                                        if (minPrice < Convert.ToInt32(product.UserPrice * digiPercent))
                                        {
                                            minPrice = Convert.ToInt32(product.UserPrice * digiPercent).ZeroConvert(1);
                                        }

                                        //update stock
                                        var updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                            true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                        if (!updateApiResult.Status)
                                        {
                                            updateApiResult = DigiUtils.NormalPriceChanging(product, digiKalaApi, product.UserPrice,
                                                minPrice, product.JumpStep, (bool)firstDigiProduct["is_active"], token);
                                            if (!updateApiResult.Status)
                                            {
                                                if (oldPrice < minPrice)
                                                {
                                                    await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                        false, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                                }
                                                db.ProductErrors.Add(new ProductErrors()
                                                {
                                                    ErrorMessage =
                                                        $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                    Store = StoreEnum.DigiKala,
                                                    Name = product.Name,
                                                    SKU = product.Dkpc,
                                                    Stock = stock.ToString(),
                                                    UpdateError = updateApiResult.Message,
                                                    ReportNumber = 5
                                                });
                                                db.SaveChanges();
                                                return;
                                            }
                                        }


                                        //conditions
                                        if (oldPrice >= minPrice)
                                        {
                                            if (!(bool)firstDigiProduct["extra"]["buy_box"]["is_buy_box_winner"] &&
                                                !(bool)firstDigiProduct["extra"]["buy_box"]["is_seller_buy_box_winner"])
                                            {
                                                int variablePrice = oldPrice;
                                                while (true)
                                                {
                                                    variablePrice -= product.JumpStep;
                                                    if (variablePrice < minPrice)
                                                    {
                                                        variablePrice = minPrice;
                                                    }
                                                    //update stock and is active
                                                    updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                        true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"],
                                                        variablePrice * 10);
                                                    if (!updateApiResult.Status)
                                                    {
                                                        db.ProductErrors.Add(new ProductErrors()
                                                        {
                                                            ErrorMessage =
                                                                $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                            Store = StoreEnum.DigiKala,
                                                            Name = product.Name,
                                                            SKU = product.Dkpc,
                                                            Stock = stock.ToString(),
                                                            UpdateError = updateApiResult.Message,
                                                            ReportNumber = 8
                                                        });
                                                        db.SaveChanges();
                                                        return;
                                                    }

                                                    //check buy box winner
                                                    var updateResultObj =
                                                        (JObject)JsonConvert.DeserializeObject(updateApiResult.Message);
                                                    if ((bool)updateResultObj["data"]["extra"]["buy_box"]["is_buy_box_winner"] ||
                                                        variablePrice == minPrice)
                                                    {
                                                        return;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //update stock and price to min price
                                            updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"],
                                                minPrice * 10);
                                            if (!updateApiResult.Status)
                                            {
                                                await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                    false, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                                db.ProductErrors.Add(new ProductErrors()
                                                {
                                                    ErrorMessage =
                                                        $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                    Store = StoreEnum.DigiKala,
                                                    Name = product.Name,
                                                    SKU = product.Dkpc,
                                                    Stock = stock.ToString(),
                                                    UpdateError = updateApiResult.Message,
                                                    ReportNumber = 9
                                                });
                                                db.SaveChanges();
                                                return;
                                            }
                                        }



                                    }));
                                }
                                else if (product.DigiStatus == 5)
                                {
                                    productChangeTasks.Add(Task.Run(async () =>
                                    {
                                        //change product stock
                                        if ((int)firstDigiProduct["stock"]["dk_reserved_stock"] >
                                        (int)firstDigiProduct["stock"]["dk_stock"])
                                        {
                                            stock = stock +
                                                                    (int)firstDigiProduct["stock"]["seller_reserved_stock"] +
                                                                    (int)firstDigiProduct["stock"]["dk_reserved_stock"] -
                                                                    (int)firstDigiProduct["stock"]["dk_stock"];
                                        }
                                        else
                                        {
                                            stock = stock +
                                                                    (int)firstDigiProduct["stock"]["seller_reserved_stock"];
                                        }

                                        var updateApiResult = await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                            true, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                        if (!updateApiResult.Status)
                                        {
                                            if (oldPrice < minPrice)
                                            {
                                                await digiKalaApi.UpdateProductAsync(product.Dkpc, stock,
                                                    false, (int)firstDigiProduct["stock"]["seller_stock"], (bool)firstDigiProduct["is_active"]);
                                            }
                                            var db = new ApplicationDbContext();
                                            db.ProductErrors.Add(new ProductErrors()
                                            {
                                                ErrorMessage =
                                                    $"در بروزرسانی کالای dkpc-{product.Dkpc} با موجودی {stock} مشکلی به وجود آمده.",
                                                Store = StoreEnum.DigiKala,
                                                Name = product.Name,
                                                SKU = product.Dkpc,
                                                Stock = stock.ToString(),
                                                UpdateError = updateApiResult.Message,
                                                ReportNumber = 10
                                            });
                                            db.SaveChanges();
                                        }
                                    }));
                                }
                            }

                            Task.WhenAll(productChangeTasks).Wait(token);

                            Dispatcher.Invoke(() =>
                            {
                                int percent = (i + 1) * 100 / mainCount;
                                DigiPercent.Text =
                                    $"({percent}%) ...";
                            });
                        }

                        watch.Stop();
                        _DigiTotalMinutes = watch.Elapsed.TotalMinutes.ToString();
                        Dispatcher.Invoke(() =>
                        {
                            DigiPercent.Text = "(تمام شد)";
                            DigiImage.Source = new BitmapImage(
                                new Uri("pack://application:,,,/Rahkar Mofid;component/Assets/Green Tick.png"));
                        });

                    }

                    var path = Utilities.GetSetupFilePath("Time.txt");
                    File.AppendAllText(path, $"Digikala time: {_DigiTotalMinutes} min\nSafirkala time: {_SafirTotalMinutes} min\nPakhsh time: {_PakhshTotalMinutes} min");
                    //finish process
                    Dispatcher.Invoke(() =>
                    {
                        if (_processMode == ProcessMode.Manual)
                        {
                            FinishedWindow finishedWindow = new();
                            finishedWindow.ShowDialog();
                        }
                        Close();
                    });
                }
                catch (OperationCanceledException)
                {
                    //ignored
                }
            });
        }

        private ProductErrors? UpdateProduct(Product product, WooCommerceApi wooCommerceApi, string jsonStringProduct,
            StoreEnum store)
        {
            var jsonProduct = (JObject)JsonConvert.DeserializeObject(jsonStringProduct);
            string? parentId = null;
            if ((string)jsonProduct["type"] == "variation")
                parentId = (string)jsonProduct["parent_id"];

            //update parent attributes
            if (parentId != null)
            {
                var parentString = wooCommerceApi.GetProductAsync(parentId).Result;
                if (!string.IsNullOrEmpty(parentString?.Replace("[", "").Replace("]", "")))
                {
                    var parentObj = JsonConvert.DeserializeObject(parentString);
                    var parentAttributes = Utilities.ChangeAttributes(JsonConvert.SerializeObject(parentObj), product.ExpiryDate,
                                            product.Guarantee, StoreEnum.SafirKala, false, true);
                    var parentResult = wooCommerceApi.UpdateProductAsync(parentId, 0, "", true, "", parentAttributes, true, null).Result;
                    if (!parentResult.Status)
                    {
                        return new ProductErrors()
                        {
                            Name = product.Name,
                            SKU = parentId,
                            Store = store,
                            ErrorMessage = $"مشکلی در آپدیت پرنت با آیدی({parentId}) در سایت به وجود آمده است.",
                            UpdateError = parentResult.Message
                        };
                    }
                }
            }

            var attributes = Utilities.ChangeAttributes(jsonStringProduct, product.ExpiryDate, product.Guarantee, store, parentId != null, false);
            var changeResult = wooCommerceApi.UpdateProductAsync((string)jsonProduct["id"], product.PakhshAndSafirStockQuantity,
                product.UserPrice.ToString(),
                store == StoreEnum.SafirKala ? product.IsSafirOff : product.IsPakhshOff,
                store == StoreEnum.SafirKala
                    ? product.SafirPriceInOff.ToString()
                    : product.PakhshPriceInOff.ToString(), attributes, false, parentId).Result;
            if (!changeResult.Status)
            {
                return new ProductErrors()
                {
                    Name = product.Name,
                    SKU = product.Sku,
                    Store = store,
                    ErrorMessage = $"مشکلی در آپدیت کالا({product.Sku}) در سایت به وجود آمده است.",
                    UpdateError = changeResult.Message
                };
            }

            return null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _source.Cancel();
        }
    }
}
