using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using StoreRobot.Models;
using StoreRobot.Utils;

namespace StoreRobot
{
    /// <summary>
    /// Interaction logic for ConnectionError.xaml
    /// </summary>
    public partial class ConnectionError : Window
    {
        private dynamic _api;
        public ConnectionError(WooCommerceApi api)
        {
            _api = api;
            InitializeComponent();
        }

        public ConnectionError(DigiKalaApi api)
        {
            _api = api;
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
            Environment.Exit(1);
        }
        #endregion

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                while (!_api.ServerRunningAsync().Result)
                {

                }
            });

            Close();
        }
    }
}
