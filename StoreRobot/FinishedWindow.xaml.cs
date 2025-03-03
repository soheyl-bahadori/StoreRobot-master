using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Google.Apis.Util;
using StoreRobot.Models;
using StoreRobot.Utils;
using Utilities = StoreRobot.Utils.Utilities;

namespace StoreRobot
{
    /// <summary>
    /// Interaction logic for FinishedWindow.xaml
    /// </summary>
    public partial class FinishedWindow : Window
    {
        private ApplicationDbContext _db = new();
        private bool _canCopy = true;
        public FinishedWindow()
        {
            InitializeComponent();
        }


        #region TopRow

        private void Rectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var finalList = new List<string>();
            foreach (var error in _db.ProductErrors.ToList())
            {
                string finalString = $"{{\n  \"DKPC\": {error.SKU},\n  \"Report Number\": {error.ReportNumber},\n  \"Name\": \"{error.Name}\",\n  \"Error Message\": \"{error.ErrorMessage}\",\n  \"First Price\": {error.FirstPrice}";

                if (!string.IsNullOrEmpty(error.ErroredPrice))
                {
                    finalString += $",\n  \"Errored Price\": {error.ErroredPrice}";
                }
                if (!string.IsNullOrEmpty(error.Stock))
                {
                    finalString += $",\n  \"Stock Quantity\": {error.Stock}";
                }
                if (!string.IsNullOrEmpty(error.UpdateError))
                {
                    finalString += $",\n  \"Update Error\": {error.UpdateError}";
                }

                finalString += "\n},";

                finalList.Add(finalString);
            }

            if (finalList.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var row in finalList)
                {
                    sb.Append(row);
                    sb.AppendLine();
                }
                sb.Remove(sb.Length - 2, 2);

                var path = Utilities.GetSetupFilePath("Process errors.txt");
                File.AppendAllText(path, sb.ToString());
            }
            foreach (var error2 in _db.ProductErrors.OrderBy(p => p.Store).ToList())
            {
                string name = error2.Store switch
                {
                    StoreEnum.SafirKala => "سفیر کالا",
                    StoreEnum.Pakhsh => "پخش",
                    StoreEnum.DigiKala => "دیجی کالا"
                };
                ErrorList.ListItems.Add(new ListItem(new Paragraph(new Run($"{name}: {error2.ErrorMessage}"))));
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ScrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_canCopy) return;
            _canCopy = false;
            var finalList = new List<string>();
            foreach (var error in _db.ProductErrors.ToList())
            {
                string finalString = $"{{\n  \"DKPC\": {error.SKU},\n  \"Report Number\": {error.ReportNumber},\n  \"Name\": \"{error.Name}\",\n  \"Error Message\": \"{error.ErrorMessage}\",\n  \"First Price\": {error.FirstPrice}";

                if (!string.IsNullOrEmpty(error.ErroredPrice))
                {
                    finalString += $",\n  \"Errored Price\": {error.ErroredPrice}";
                }
                if (!string.IsNullOrEmpty(error.Stock))
                {
                    finalString += $",\n  \"Stock Quantity\": {error.Stock}";
                }
                if (!string.IsNullOrEmpty(error.UpdateError))
                {
                    finalString += $",\n  \"Update Error\": {error.UpdateError}";
                }

                finalString += "\n},";

                finalList.Add(finalString);

                /*string finalString = error.Name + ": " + error.ErrorMessage;
                if (!string.IsNullOrEmpty(error.UpdateError))
                {
                    finalString = string.IsNullOrEmpty(error.ErroredPrice)
                        ? $"DKPC-{error.SKU}: Stock={error.Stock}, Error message=\"{error.UpdateError}\""
                        : $"DKPC-{error.SKU}: Price={error.ErroredPrice}, Stock={error.Stock}, Error message=\"{error.UpdateError}\"";
                }

                finalList.Add(finalString);*/
            }

            if (finalList.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var row in finalList)
                {
                    sb.Append(row);
                    sb.AppendLine();
                }
                sb.Remove(sb.Length - 2, 2);
                Clipboard.SetText(sb.ToString());
                await Task.Run(() => Thread.Sleep(10000));
            }
            _canCopy = true;
        }
    }
}
