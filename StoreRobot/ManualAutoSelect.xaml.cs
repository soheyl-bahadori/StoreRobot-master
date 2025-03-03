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

namespace StoreRobot
{
    /// <summary>
    /// Interaction logic for ManualAutoSelect.xaml
    /// </summary>
    public partial class ManualAutoSelect : Window
    {
        public ManualAutoSelect()
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
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        #endregion

        private void ManualButton_Click(object sender, RoutedEventArgs e)
        {
            if (ManualCheckBox.IsChecked != null && ManualCheckBox.IsChecked.Value) return;
            ManualCheckBox.IsChecked = true;
            AutoCheckBox.IsChecked = false;
            if (ContinueButton.IsEnabled) return;
            ContinueButton.IsEnabled = true;
        }

        private void AutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (AutoCheckBox.IsChecked != null && AutoCheckBox.IsChecked.Value) return;
            AutoCheckBox.IsChecked = true;
            ManualCheckBox.IsChecked = false;
            if (ContinueButton.IsEnabled) return;
            ContinueButton.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ContinueButton.IsEnabled = false;
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (ManualCheckBox.IsChecked.Value)
            {
                var manualWindow = new ManualWindow();   //Create form if not created
                manualWindow.Closed += (s, arg) =>
                {
                    if (manualWindow.IsTotallyClosed)
                    {
                        Close();
                    }
                    else if(manualWindow.ChangingProcessShown)
                    {
                        manualWindow.ChangingProcessWindow.Closed += (s, arg) => Close();
                    }
                    else
                    {
                        Show();
                    }
                };

                manualWindow.Show();
                Hide();
            }
            else
            {
                var autoWindow = new AutoWindow();   //Create form if not created
                autoWindow.Closed += (s, arg) =>
                {
                    if (autoWindow.IsTotallyClosed)
                    {
                        Close();
                    }
                    else if (autoWindow.AutoChangingProcessShown)
                    {
                        autoWindow.AutoChangingProcessWindow.Closed += (s, arg) => Close();
                    }
                    else
                    {
                        Show();
                    }
                };

                autoWindow.Show();
                Hide();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
