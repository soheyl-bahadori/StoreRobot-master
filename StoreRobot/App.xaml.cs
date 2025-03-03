using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using StoreRobot.Utils;

namespace StoreRobot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            /*using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress("rahkarmofiderrorsender@gmail.com");
                mail.To.Add("sheet@rahkarmofid.com");
                mail.To.Add("hesamjavadi.023@gmail.com");
                mail.Subject = "error occurred";
                mail.Body = "<h1>please check error in server.</h1>";
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("rahkarmofiderrorsender@gmail.com", "2=Nh^%LgXZng");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }*/

            var errorMessage = new ErrorMessage($"Something went wrong: \n{e.Exception.Message}");
            errorMessage.ShowDialog();
            MessageBox.Show(e.Exception.ToString());
            Environment.Exit(0);
            e.Handled = true;
        }
        private static Mutex _mutex = null;

        protected void OnStartup(object sender, StartupEventArgs startupEventArgs)
        {
            const string appName = "MyAppName";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                //app is already running! Exiting the application  
                //Application.Current.Shutdown();
            }
        }
    }
}
