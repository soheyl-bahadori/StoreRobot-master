using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoreRobot.Models;
using StoreRobot.Utils;

namespace StoreRobot
{
    /// <summary>
    /// Interaction logic for ManualWindow.xaml
    /// </summary>
    public partial class ManualWindow : Window
    {
        private ApplicationDbContext _db = new();
        public bool IsTotallyClosed;
        public bool ChangingProcessShown = false;
        public ChangingProcessWindow ChangingProcessWindow;
        CancellationTokenSource _source = new();
        private List<string> _changedProductSafirPakhsh = new();
        private List<string> _changedProductDigiKala = new();
        private bool _canCopy = true;
        public ManualWindow()
        {
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
            IsTotallyClosed = true;
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        #endregion

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GoogleSheetApi googleSheetApi = new GoogleSheetApi(_db);
            await googleSheetApi.AddSheetToDbAsync();
            CancellationToken token = _source.Token;
            try
            {

                var tasks = new List<Task>();

                //pakhsh and safir
                tasks.Add(Task.Run(() =>
                {
                    var db = new ApplicationDbContext();

                    //define api instances
                    WooCommerceApi safirApi = new WooCommerceApi(StoreEnum.SafirKala);
                    WooCommerceApi pakhshApi = new WooCommerceApi(StoreEnum.Pakhsh);

                    int syncCount = 100;
                    int count = db.Products.Count() % syncCount != 0
                        ? db.Products.Count() / syncCount + 1
                        : db.Products.Count() / syncCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        var products = db.Products.Skip(i * syncCount).Take(syncCount)
                            .Where(p => !string.IsNullOrEmpty(p.Name)).ToList();

                        //get jsons
                        var safirJsonProductTask = safirApi.GetProductAsync(products.Select(p => p.Sku).ToList());
                        var pakhshJsonProductTask = pakhshApi.GetProductAsync(products.Select(p => p.Sku).ToList());
                        var safirJsonProduct = safirJsonProductTask.Result;
                        var pakhshJsonProduct = pakhshJsonProductTask.Result;
                        if (safirJsonProduct == null || pakhshJsonProduct == null)
                        {
                            continue;
                        }
                        JArray safirJsonResult = (JArray)JsonConvert.DeserializeObject(safirJsonProduct);
                        JArray pakhshJsonResult = (JArray)JsonConvert.DeserializeObject(pakhshJsonProduct);
                        CheckProductsChange(products, safirJsonResult, pakhshJsonResult);

                        //finish
                        Dispatcher.Invoke(() =>
                        {
                            SafirPakhshPercent.Text = ((i + 1) * 100 / count).ToString();
                        });
                    }
                }, token));

                //Digikala
                tasks.Add(Task.Run(async () =>
                {
                    var db = new ApplicationDbContext();

                    //define api instances
                    DigiKalaApi digiKalaApi = new();

                    int syncCount = 10;
                    int count = db.Products.Count() % syncCount != 0
                        ? db.Products.Count() / syncCount + 1
                        : db.Products.Count() / syncCount;
                    for (int i = 0; i < count; i++)
                    {
                        if (token.IsCancellationRequested) return;
                        var products = db.Products.Skip(i * syncCount).Take(syncCount)
                            .Where(p => !string.IsNullOrEmpty(p.Name)).ToList();

                        var changeTasks = new List<Task>();

                        foreach (var product in products)
                        {
                            if (token.IsCancellationRequested) return;
                            changeTasks.Add(Task.Run(() =>
                            {
                                if (string.IsNullOrEmpty(product.Dkpc)) return;
                                string? productString = digiKalaApi.GetProductAsync(product.Dkpc).Result;
                                if (productString == null) return;
                                if (!Utilities.IsProductChanged(productString, product, StoreEnum.DigiKala)) return;
                                string finalName = $"{product.Name}‌ کد DKPC-{product.Dkpc} (دیجی کالا)";
                                Dispatcher.Invoke(() =>
                                {
                                    DigiKalaProductChangesList.ListItems.Add(
                                        new ListItem(new Paragraph(new Run(finalName))));
                                });
                                _changedProductDigiKala.Add(finalName);
                            }));
                        }

                        await Task.WhenAll(changeTasks);

                        //finish
                        Dispatcher.Invoke(() =>
                        {
                            DigiKalaPercent.Text = ((i + 1) * 100 / count).ToString();
                        });
                    }
                }, token));

                await Task.WhenAll(tasks);

                //Loading Text
                LoadingText.Text = ProductChangesList.ListItems.Count != 0
                    ? "تمام تغییرات پیدا شدند!"
                    : "تغییراتی در sheet یافت نشد!";
            }
            catch (OperationCanceledException)
            {
                //ignored
            }
        }

        void CheckProductsChange(List<Product> products, JArray safirProductsJson, JArray pakhshProductsJson)
        {
            var items = new List<ListItem>();
            foreach (var product in products)
            {
                if (_source.Token.IsCancellationRequested)
                {
                    return;
                }

                //check safir changes
                bool isSafirChanged = false;
                var safirProductJson = safirProductsJson?.FirstOrDefault(s => (string)s["sku"] == product.Sku);
                if (safirProductJson != null)
                    isSafirChanged = Utilities.IsProductChanged(JsonConvert.SerializeObject(safirProductJson), product,
                        StoreEnum.SafirKala);


                //check safir changes
                bool isPakhshChanged = false;
                var pakhshProductJson = pakhshProductsJson?.FirstOrDefault(p => (string)p["sku"] == product.Sku);
                if (pakhshProductJson != null)
                    isPakhshChanged = Utilities.IsProductChanged(JsonConvert.SerializeObject(pakhshProductJson), product,
                        StoreEnum.Pakhsh);

                if (isSafirChanged || isPakhshChanged)
                {
                    string finalName = $"{product.Name}‌ کد {product.Sku}";
                    if (isSafirChanged)
                    {
                        finalName += " (سفیر کالا)";
                    }

                    if (isPakhshChanged)
                    {
                        finalName += " (پخش)";
                    }
                    Dispatcher.Invoke(() =>
                    {
                        items.Add(new ListItem(new Paragraph(new Run(finalName))));
                    });
                    _changedProductSafirPakhsh.Add(finalName);
                }
            }
            Dispatcher.Invoke(() =>
            {
                ProductChangesList.ListItems.AddRange(items);
            });
        }

        private async void ScrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_canCopy) return;
            _canCopy = false;
            var finalList = new List<string>();
            finalList.Add("تغییرات سفیر و پخش:");
            finalList.AddRange(_changedProductSafirPakhsh);
            finalList.Add("\n\nتغییرات دیجی کالا:");
            finalList.AddRange(_changedProductDigiKala);

            StringBuilder sb = new StringBuilder();
            foreach (var row in finalList)
            {
                sb.Append(row);
                sb.AppendLine();
            }
            sb.Remove(sb.Length - 1, 1);
            Clipboard.SetText(sb.ToString());
            await Task.Run(() => Thread.Sleep(10000));
            _canCopy = true;

        }

        private void SafirButton_Click(object sender, RoutedEventArgs e)
        {
            SafirCheckBox.IsChecked = !SafirCheckBox.IsChecked;
            StartButton.IsEnabled = SafirCheckBox.IsChecked.Value || PakhshCheckBox.IsChecked.Value || DigiCheckBox.IsChecked.Value;
        }
        private void PakhshButton_Click(object sender, RoutedEventArgs e)
        {
            PakhshCheckBox.IsChecked = !PakhshCheckBox.IsChecked;
            StartButton.IsEnabled = SafirCheckBox.IsChecked.Value || PakhshCheckBox.IsChecked.Value || DigiCheckBox.IsChecked.Value;
        }
        private void DigiButton_Click(object sender, RoutedEventArgs e)
        {
            DigiCheckBox.IsChecked = !DigiCheckBox.IsChecked;
            StartButton.IsEnabled = SafirCheckBox.IsChecked.Value || PakhshCheckBox.IsChecked.Value || DigiCheckBox.IsChecked.Value;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ChangingProcessWindow = new(SafirCheckBox.IsChecked.Value, PakhshCheckBox.IsChecked.Value,
                DigiCheckBox.IsChecked.Value, ProcessMode.Manual);
            ChangingProcessWindow.Show();
            ChangingProcessShown = true;
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _source.Cancel();
        }

    }
}
