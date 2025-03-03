using StoreRobot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for Configurations.xaml
    /// </summary>
    public partial class Configurations : Window
    {
        public Configurations()
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
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            var getConfigurationData = Utilities.GetSettings();

            DigikalaPassword.Text = getConfigurationData.DigikalaPassword;

            emailReceiverUserName.Text = getConfigurationData.EmailReceiverSetting.emailReceiverUserName;
            emailReceiverpassword.Text = getConfigurationData.EmailReceiverSetting.emailReceiverpassword;

            DigiKalaApiKey.Text = getConfigurationData.ApiKeys.DigiKala;
            SafirKalaApiKey.Text = getConfigurationData.ApiKeys.SafirKala;
            PakhshApiKey.Text = getConfigurationData.ApiKeys.Pakhsh;

            From.Text = getConfigurationData.DigiTest.From.ToString();
            Count.Text = getConfigurationData.DigiTest.Count.ToString();

            IsAutomate.IsChecked = getConfigurationData.DigiKalaRateLimitSetting.IsAutomate;
            AutomateMinutesCoolDown.Text = getConfigurationData.DigiKalaRateLimitSetting.AutomateMinutesCoolDown.ToString();
            ManualMinutesCoolDown.Text = getConfigurationData.DigiKalaRateLimitSetting.ManualMinutesCoolDown.ToString();
            ManualRequestLimitCount.Text = getConfigurationData.DigiKalaRateLimitSetting.ManualRequestLimitCount.ToString();

            var digipercent4 = getConfigurationData.DigiPercent.FirstOrDefault(d => d.Key == 4).Value.ToString();
            var digipercent6 = getConfigurationData.DigiPercent.FirstOrDefault(d => d.Key == 6).Value.ToString();
            var digipercent7 = getConfigurationData.DigiPercent.FirstOrDefault(d => d.Key == 7).Value.ToString();
            var digipercent8 = getConfigurationData.DigiPercent.FirstOrDefault(d => d.Key == 8).Value.ToString();
            var digipercent9 = getConfigurationData.DigiPercent.FirstOrDefault(d => d.Key == 9).Value.ToString();

            DigiPercent4.Text = !string.IsNullOrEmpty(digipercent4) ? digipercent4 : null;
            DigiPercent6.Text = !string.IsNullOrEmpty(digipercent6) ? digipercent6 : null;
            DigiPercent7.Text = !string.IsNullOrEmpty(digipercent7) ? digipercent7 : null;
            DigiPercent8.Text = !string.IsNullOrEmpty(digipercent8) ? digipercent8 : null;
            DigiPercent9.Text = !string.IsNullOrEmpty(digipercent9) ? digipercent9 : null;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Status.Content = "";
            var newConfigurationData = Utilities.GetSettings();

            newConfigurationData.DigikalaPassword = DigikalaPassword.Text;

            newConfigurationData.EmailReceiverSetting.emailReceiverUserName= emailReceiverUserName.Text;
            newConfigurationData.EmailReceiverSetting.emailReceiverpassword = emailReceiverpassword.Text;

            newConfigurationData.ApiKeys.DigiKala=DigiKalaApiKey.Text;
            newConfigurationData.ApiKeys.SafirKala = SafirKalaApiKey.Text;
            newConfigurationData.ApiKeys.Pakhsh = PakhshApiKey.Text;

            newConfigurationData.DigiTest.From = Convert.ToInt32(From.Text);
            newConfigurationData.DigiTest.Count = Convert.ToInt32(Count.Text);


            newConfigurationData.DigiKalaRateLimitSetting.IsAutomate = IsAutomate.IsChecked.Value;
            newConfigurationData.DigiKalaRateLimitSetting.AutomateMinutesCoolDown = string.IsNullOrEmpty(AutomateMinutesCoolDown.Text) ? null : Convert.ToInt32(AutomateMinutesCoolDown.Text);
            newConfigurationData.DigiKalaRateLimitSetting.ManualMinutesCoolDown = string.IsNullOrEmpty(ManualMinutesCoolDown.Text) ? null : Convert.ToInt32(ManualMinutesCoolDown.Text);
            newConfigurationData.DigiKalaRateLimitSetting.ManualRequestLimitCount = string.IsNullOrEmpty(ManualRequestLimitCount.Text) ? null : Convert.ToInt32(ManualRequestLimitCount.Text);

            newConfigurationData.DigiPercent = new List<(int Key, double Value)>
            {
                (4,Convert.ToDouble(DigiPercent4.Text)),
                (6,Convert.ToDouble(DigiPercent6.Text)),
                (7,Convert.ToDouble(DigiPercent7.Text)),
                (8,Convert.ToDouble(DigiPercent8.Text)),
                (9,Convert.ToDouble(DigiPercent9.Text))
            };

            var ret = Utilities.EditSettings(newConfigurationData);
            if (ret)
            {
                Status.Foreground = new SolidColorBrush(Colors.Green);
                Status.Content = "با موفقیت ویرایش شد";
                LoadData();
            }
            else
            {
                Status.Foreground = new SolidColorBrush(Colors.Red);
                Status.Content = "خطایی در ویرایش رخ داده است";
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[.][0-9]+$|^[0-9]*[.]{0,1}[0-9]*$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }
    }
}
