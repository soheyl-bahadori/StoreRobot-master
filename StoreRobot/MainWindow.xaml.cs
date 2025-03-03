using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using StoreRobot.Models;
using StoreRobot.Utils;

namespace StoreRobot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ApplicationDbContext _db = new();
        public MainWindow()
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
            Environment.Exit(1);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion


        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //throw new Exception("Test error");
            _db.Database.EnsureCreated();
        }

        private void WebHookButton_Click(object sender, RoutedEventArgs e)
        {
            WebHookButton.IsEnabled = false;
            WebHookManagerWindow webHookManagerWindow = new();
            webHookManagerWindow.Show();
            webHookManagerWindow.Closed += (s, arg) =>
            {
                this.Activate();
                WebHookButton.IsEnabled = true;
            };
        }
        private void UpdateStores_Click(object sender, RoutedEventArgs e)
        {
            UpdateStores.IsEnabled = false;
            ManualAutoSelect manualAutoSelectWindow = new();
            manualAutoSelectWindow.Show();
            manualAutoSelectWindow.Closed += (s, arg) =>
            {
                this.Activate();
                UpdateStores.IsEnabled = true;
            };
        }

        private void ConfigurationButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationButton.IsEnabled = false;
            Configurations configurations = new();
            configurations.Show();
            configurations.Closed += (s, arg) =>
            {
                this.Activate();
                ConfigurationButton.IsEnabled = true;
            };
        }
    }
}
