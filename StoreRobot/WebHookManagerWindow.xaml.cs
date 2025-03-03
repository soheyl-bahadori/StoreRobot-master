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
    /// Interaction logic for WebHookManagerWindow.xaml
    /// </summary>
    public partial class WebHookManagerWindow : Window
    {
        CancellationTokenSource _source = new();
        public WebHookManagerWindow()
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

        private void Window_Closed(object sender, EventArgs e)
        {
            _source.Cancel();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            CancellationToken token = _source.Token;
            await Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        //check safir webhook
                        WooCommerceApi safirApi = new(StoreEnum.SafirKala);
                        var safirResult = safirApi.GetWebHooksAsync().Result;
                        if (string.IsNullOrEmpty(safirResult))
                        {
                            continue;
                        }
                        var safirWebhooks = (JArray)JsonConvert.DeserializeObject(safirResult);
                        var safirWebhook = safirWebhooks.FirstOrDefault(s =>
                            (string) s["name"] == "RahkarRobotApplicationWebHook");
                        if ((string)safirWebhook["status"] != "active")
                        {
                            safirApi.UpdateWebHookAsync((string) safirWebhook["id"], "active").Wait();
                        }

                        //check pakhsh webhook
                        WooCommerceApi pakhshApi = new(StoreEnum.Pakhsh);
                        var pakhshResult = pakhshApi.GetWebHooksAsync().Result;
                        if (string.IsNullOrEmpty(pakhshResult))
                        {
                            continue;
                        }
                        var pakhshWebhooks = (JArray)JsonConvert.DeserializeObject(pakhshResult);
                        var pakhshWebhook = pakhshWebhooks.FirstOrDefault(p =>
                            (string)p["name"] == "RahkarRobotApplicationWebHook");
                        if ((string)pakhshWebhook["status"] != "active")
                        {
                            pakhshApi.UpdateWebHookAsync((string)pakhshWebhook["id"], "active").Wait();
                        }

                        Task.Delay(TimeSpan.FromSeconds(3), token).Wait(token);
                    }
                }
                catch (OperationCanceledException)
                {
                    //ignored
                }
            });
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            WarningMessage warningMessage = new();
            var dialogResult = warningMessage.ShowDialog();
            if (!dialogResult.Value) return;

            _source.Cancel();

            StopButton.IsEnabled = false;

            //stop safir webhook
            WooCommerceApi safirApi = new(StoreEnum.SafirKala);
            var safirResult = await safirApi.GetWebHooksAsync();
            if (string.IsNullOrEmpty(safirResult))
            {
                return;
            }
            var safirWebhooks = (JArray)JsonConvert.DeserializeObject(safirResult);
            var safirWebhook = safirWebhooks.FirstOrDefault(s =>
                (string)s["name"] == "RahkarRobotApplicationWebHook");
            if ((string)safirWebhook["status"] != "disabled")
            {
                await safirApi.UpdateWebHookAsync((string)safirWebhook["id"], "disabled");
            }

            //stop pakhsh webhook
            WooCommerceApi pakhshApi = new(StoreEnum.Pakhsh);
            var pakhshResult = await pakhshApi.GetWebHooksAsync();
            if (string.IsNullOrEmpty(pakhshResult))
            {
                return;
            }
            var pakhshWebhooks = (JArray)JsonConvert.DeserializeObject(pakhshResult);
            var pakhshWebhook = pakhshWebhooks.FirstOrDefault(p =>
                (string)p["name"] == "RahkarRobotApplicationWebHook");
            if ((string)pakhshWebhook["status"] != "disabled")
            {
                await pakhshApi.UpdateWebHookAsync((string)pakhshWebhook["id"], "disabled");
            }

            Close();
        }
    }
}
