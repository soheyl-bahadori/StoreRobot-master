using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using StoreRobot.Models;
using Cookie = OpenQA.Selenium.Cookie;

namespace StoreRobot.Utils
{
    public class SeleniumUtils
    {
        private IWebDriver _driver;
        private ApplicationDbContext _db = new();
        public SeleniumUtils()
        {
            ChromeDriverInstaller installer = new();
            installer.Install(installer.GetChromeVersion().Result).Wait();

            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddUserProfilePreference("profile.managed_default_content_settings.images", 2);
            chromeOptions.AddArgument("no-sandbox");
            //chromeOptions.AddArguments("--headless");
            _driver = new ChromeDriver(driverService, chromeOptions);
            _driver.Manage().Window.Maximize();
        }

        public async Task<CommissionStatus> UpdateCommissions(ApplicationDbContext db)
        {
            try
            {
                _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10000);
                _driver.Navigate().GoToUrl("https://seller.digikala.com/account/login/");
                WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
                wait.Until(ExpectedConditions.ElementExists(By.Name("userName")));
            }
            catch
            {
                //ignored
            }

            //fill user and pass in login page
            if (Utilities.IsElementPresent(_driver, By.Name("userName")))
            {
                //username page
                var userNameElement = _driver.FindElement(By.Name("userName"));
                userNameElement.SendKeys("chapardarkala@gmail.com");
                var loginButton = _driver.FindElement(By.XPath("//button[@type='submit']"));
                loginButton.Submit();
                Thread.Sleep(2000);

                //password page
                //string? password = GetPassword();
                string? password = Utilities.GetSettings().DigikalaPassword;
                if (string.IsNullOrEmpty(password))
                {
                    return new CommissionStatus()
                    {
                        Result = false,
                        Message =
                            @"لطفا در دسکتاپ خود تکست فایلی با نام ""password.txt"" بسازید . رمز عبور دیجی کالا خود را در آن قرار دهید."
                    };
                }
                var passwordElement = _driver.FindElement(By.Name("password"));
                passwordElement.SendKeys(password);
                loginButton = _driver.FindElement(By.XPath("//button[@type='submit']"));
                loginButton.Submit();
                Thread.Sleep(2000);

                //if password wrong 
                if (Utilities.IsElementPresent(_driver, By.Name("password")))
                {
                    return new CommissionStatus()
                    {
                        Result = false,
                        Message = @"رمز عبوری که در فایل ""password.txt"" وجود دارد اشتباه است لطفا آن را اصلاح نمایید."
                    };
                }

            }

            _driver.Navigate().GoToUrl("https://seller.digikala.com/sellercommission/");

            var myProductsButton = _driver.FindElement(By.XPath(@"//*[@id=""searchForm""]/div/div/div/div[5]/label"));
            myProductsButton.Click();
            Thread.Sleep(7000);

            var commissionElements = _driver.FindElements(By.XPath(
                @"/html/body/main/div/div/div[2]/ul/li[1]/div[1]/div[2]/div/div/div/div[2]/table/tbody/tr"));
            var commissions = new List<Commission>();
            foreach (var commissionElement in commissionElements)
            {
                List<IWebElement> lstTdElem = new List<IWebElement>(commissionElement.FindElements(By.TagName("td")));
                commissions.Add(new Commission()
                {
                    Name = lstTdElem[2].Text,
                    CommissionPercent = Convert.ToDouble(lstTdElem[4].Text.ToEnglish()) / 100
                });
            }

            db.RemoveRange(db.Commissions);
            await db.AddRangeAsync(commissions);
            await db.SaveChangesAsync();

            return new CommissionStatus()
            {
                Result = true
            };
        }

        public BuyBoxResult IsBuyBoxWinner(string dkp, string title, bool getLeastPrice)
        {
            try
            {
                _driver.Navigate().GoToUrl($"https://www.digikala.com/product/dkp-{dkp}/");

                //check if product loaded
                if (Utilities.IsElementPresent(_driver, By.XPath(
                        @"//p[contains(text(), ""غیرفعال"")]")))
                {
                    return new BuyBoxResult() { Result = false, ErrorMessage = "این کالا غیرفعال است یا دسترسی به آن وجود ندارد"};
                }

                var titleStrings = title.Split("|").Select(t => t.Trim()).ToList();
                if (titleStrings.Count() == 3)
                {
                    if (titleStrings[1].IsNumeric() &&
                        Utilities.IsElementPresent(_driver, By.XPath(@"//input[@placeholder=""اندازه""]")))
                    {
                        //click size dropdown
                        var sizeDropDown = _driver.FindElement(By.XPath(@"//input[@placeholder=""اندازه""]"));
                        Utilities.ScrollToElement(_driver, sizeDropDown);
                        sizeDropDown.Click();
                        Thread.Sleep(100);

                        //click dropdown
                        var sizeSelect = _driver.FindElement(By.XPath(
                            $@"//input[@placeholder=""اندازه""]/following::ul[1]/li[contains(text(), ""{titleStrings[2].ToFarsi()}"")]"));
                        sizeSelect.Click();
                    }
                    else if (_driver.FindElements(By.XPath(
                                     @"//*[@id=""__next""]/div[1]/div[3]/div[3]/div[2]/div[2]/div[2]/div[2]/div[1]/div[3]/div[2]/div"))
                                 .Count > 1)
                    {
                        var colorButton =
                            _driver.FindElement(By.XPath($@"//span[contains(text(), ""{titleStrings[1]}"")]/parent::div"));
                        colorButton.Click();
                        Thread.Sleep(100);
                    }
                }

                //select warranty
                //...

                if (Utilities.IsElementPresent(_driver, By.Id("sellerSection")))
                {
                    var leastPrice = 0;
                    if (getLeastPrice)
                    {
                        if (Utilities.IsElementPresent(_driver, By.XPath(@"//*[@id=""sellerSection""]/div/span/span")))
                        {
                            var showMore = _driver.FindElement(By.XPath(@"//*[@id=""sellerSection""]/div/span/span"));
                            try
                            {
                                Actions actions = new Actions(_driver);
                                actions.MoveToElement(
                                    _driver.FindElement(By.XPath("//*[@id=\"__next\"]/div[1]/div[3]/div[3]/div[2]/div[6]")));
                                actions.Perform();
                            }
                            catch
                            {
                                // ignored
                            }

                            showMore.Click();
                        }

                        List<int> prices = _driver
                            .FindElements(
                                By.XPath(@"//*[@id=""sellerSection""]/div/div/div/div[1]/div[2]/div[2]/div[1]/a/p[not(text()=""پخش کالای مرکزی"")]/parent::a/parent::div/parent::div/parent::div/parent::div/parent::div/div[2]/div[1]/div/div/div/div[1]/span | //*[@id=""sellerSection""]/div/div/div/div[1]/div[2]/div[2]/div[1]/a/p[not(text()='پخش کالای مرکزی')]/parent::a/parent::div/parent::div/parent::div/parent::div/parent::div/parent::div/div[1]/div[2]/div[1]/div/div/span"))
                            .Select(t => Convert.ToInt32(t.Text.Trim().Replace(",", "").ToEnglish())).ToList();
                        leastPrice = prices.Min();
                    }

                    var buyBoxName = _driver.FindElement(By.XPath(
                            @"//*[@id=""__next""]/div[1]/div[3]/div[3]/div[2]/div[2]/div[2]/div[2]/div[3]/div[1]/div[4]/a/div/div/div[2]/div/div[1]/span/p"))
                        .Text;
                    
                    if (buyBoxName == "پخش کالای مرکزی")
                    {
                        return new BuyBoxResult()
                            {Result = true, HaveCompetitor = true, IsBuyBoxWinner = true, LeastPrice = leastPrice};
                    }
                    else
                    {
                        return new BuyBoxResult()
                            {Result = true, HaveCompetitor = true, IsBuyBoxWinner = false, LeastPrice = leastPrice };
                    }
                }
                else
                {
                    return new BuyBoxResult() {Result = true, HaveCompetitor = false, IsBuyBoxWinner = true};
                }
            }
            catch (Exception e)
            {
                return new BuyBoxResult() {Result = false, ErrorMessage = e.Message};
            }
        }

       /* public string? GetPassword()
        {
            try
            {
                var path = Utilities.GetSetupFilePath("password.txt");

                if (!File.Exists(path) || string.IsNullOrEmpty(File.ReadAllText(path)))
                {
                    File.AppendAllText(path, "");
                    return null;
                }
                return File.ReadAllText(path).Trim();
            }
            catch
            {
                return null;
            }
        }*/

        private void GoToPage(string url)
        {
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(3);
            try
            {
                _driver.Navigate().GoToUrl(url);
            }
            catch 
            {
               //ignore
            }
        }

        public void Exit()
        {
            _driver.Quit();
        }
    }
}
