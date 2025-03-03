using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoreRobot.Migrations;
using StoreRobot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace StoreRobot.Utils
{
    public class DigiKalaApi
    {
        private string _apiKey;
        private SetupSettings _setupSettings;
        private ApplicationDbContext _db = new();

        public DigiKalaApi()
        {
            _setupSettings = Utilities.GetSettings();
            _apiKey = _setupSettings.ApiKeys.DigiKala;
        }

        public async Task<bool> ServerRunningAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("http://www.digikala.com");

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
            //try
            //{
            //    using var httpClient = new HttpClient();
            //    using var request = new HttpRequestMessage(new HttpMethod("GET"),
            //        "https://seller.digikala.com/api/v1/variants/?search[id]=910610");
            //    request.Headers.TryAddWithoutValidation("Authorization", _apiKey);

            //    _ = await httpClient.SendAsync(request).ConfigureAwait(false);

            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
        }

        public async Task<string?> GetProductAsync(string dkpc)
        {
            while (true)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    using var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://seller.digikala.com/api/v1/variants/?search[id]={dkpc}");
                    request.Headers.TryAddWithoutValidation("Authorization", _apiKey);

                    var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    var result = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                    if (await RateLimit(response))
                        return await GetProductAsync(dkpc);
                    else if (!response.IsSuccessStatusCode)
                        return null;

                    try
                    {
                        var deserialize = (JObject)JsonConvert.DeserializeObject(result);

                        if ((string)deserialize["status"] != "ok") return null;

                        return JsonConvert.SerializeObject(deserialize["data"]["items"][0]);
                    }
                    catch
                    {
                        return null;
                    }
                }
                catch
                {
                    while (!ServerRunningAsync().Result)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var serverError = new ConnectionError(this);
                            serverError.ShowDialog();
                        });
                    }
                }
            }

        }

        //need refactor
        public async Task<Result> UpdateProductAsync(string dkpc, int stockQuantity, bool isActive, int oldStock, bool oldIsActive, int? price = null)
        {
            while (true)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var request = new HttpRequestMessage(new HttpMethod("PUT"), $"https://seller.digikala.com/api/v1/variants/{dkpc}/");
                    request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
                    if (!oldIsActive && price != null)
                    {
                        request = new HttpRequestMessage(new HttpMethod("PUT"), $"https://seller.digikala.com/api/v1/variants/{dkpc}/");
                        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
                        request.Content = new StringContent($"{{\n    \"seller_stock\": {stockQuantity},\n    \"price\": {price}\n}}");
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

                        var response1 = await httpClient.SendAsync(request).ConfigureAwait(false);
                        using var streamReader1 = new StreamReader(await response1.Content.ReadAsStreamAsync().ConfigureAwait(false));
                        var result1 = await streamReader1.ReadToEndAsync().ConfigureAwait(false);

                        if (await RateLimit(response1))
                            return await UpdateProductAsync(dkpc,stockQuantity,isActive,oldStock,oldIsActive,price);
                        else if (!response1.IsSuccessStatusCode)
                        {
                            var resultObj = (JObject)JsonConvert.DeserializeObject(result1);
                            result1 = JsonConvert.SerializeObject(resultObj["data"]);


                            return new Result()
                            { Status = false, Message = result1.Replace(@"""", "").Replace("{", "").Replace("}", "") };
                        }
                    }
                    if (oldStock == 0)
                    {
                        request = new HttpRequestMessage(new HttpMethod("PUT"), $"https://seller.digikala.com/api/v1/variants/{dkpc}/");
                        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
                        request.Content = new StringContent($"{{\n    \"seller_stock\": {stockQuantity}\n}}");
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

                        var response1 = await httpClient.SendAsync(request).ConfigureAwait(false);
                        using var streamReader1 = new StreamReader(await response1.Content.ReadAsStreamAsync().ConfigureAwait(false));
                        var result1 = await streamReader1.ReadToEndAsync().ConfigureAwait(false);
                        
                        if (await RateLimit(response1))
                            return await UpdateProductAsync(dkpc, stockQuantity, isActive, oldStock, oldIsActive, price);
                        else if (!response1.IsSuccessStatusCode)
                        {
                            var resultObj = (JObject)JsonConvert.DeserializeObject(result1);
                            result1 = JsonConvert.SerializeObject(resultObj["data"]);


                            return new Result()
                            { Status = false, Message = result1.Replace(@"""", "").Replace("{", "").Replace("}", "") };
                        }
                    }

                    price = price?.ZeroConvert(2);
                    httpClient = new HttpClient();
                    request = new HttpRequestMessage(new HttpMethod("PUT"), $"https://seller.digikala.com/api/v1/variants/{dkpc}/");
                    request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
                    request.Content = price != null ?
                        new StringContent($"{{\n    \"seller_stock\": {stockQuantity},\n    \"is_active\": {isActive.ToString().ToLower()},\n    \"price\": {price}\n}}") :
                        new StringContent($"{{\n    \"seller_stock\": {stockQuantity},\n    \"is_active\": {isActive.ToString().ToLower()}\n}}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

                    var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    var result = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                    if (await RateLimit(response))
                        return await UpdateProductAsync(dkpc, stockQuantity, isActive, oldStock, oldIsActive, price);
                    else if (!response.IsSuccessStatusCode)
                    {
                        var resultObj = (JObject)JsonConvert.DeserializeObject(result);
                        result = JsonConvert.SerializeObject(resultObj["data"]);


                        return new Result()
                        { Status = false, Message = result.Replace(@"""", "").Replace("{", "").Replace("}", "") };
                    }
                    return new Result() { Status = true, Message = result };
                }
                catch
                {
                    while (!ServerRunningAsync().Result)
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            var serverError = new ConnectionError(this);
                            serverError.ShowDialog();
                        });
                    }
                }
            }

        }

        public async Task<CommissionStatus> UpdateCommisionAsync()
        {
            var loginToken = await GetDigikalaLoginTokenAsync();

            #region Get Login Token and Save it
            if (loginToken == null)
            {
                var digiKalaSettings = Utilities.GetSettings();

                string? verificationCode = null;

                var sendActivationCodeStatus = await SendActivtionCode(digiKalaSettings.DigikalaLoginSetting.digikalaUserName);
                if (sendActivationCodeStatus)
                {
                    //Wait for 5 second to ensure email sended
                    Thread.Sleep(5000);
                    verificationCode = await GetVerificationCodeAsync(digiKalaSettings);

                    if (verificationCode != null)
                    {
                        var loginResponse = await Login(digiKalaSettings.DigikalaLoginSetting.digikalaUserName, verificationCode);

                        if (loginResponse != null)
                        {
                            _db.DigikalaLoginToken.Add(loginResponse);
                            try
                            {
                                _db.SaveChanges();
                                loginToken = loginResponse;
                            }
                            catch (Exception)
                            {
                                return new CommissionStatus
                                {
                                    Result = false,
                                    Message = "خطایی در ذخیره سازی توکن ورود دیجی کالا رخ داده است!"
                                };
                            }

                        }
                    }
                    else
                    {
                        return new CommissionStatus
                        {
                            Result = false,
                            Message = "خطایی در گرفتن کد تایید از ایمیل رخ داده است!"
                        };
                    }
                }
                else
                {
                    return new CommissionStatus
                    {
                        Result = false,
                        Message = "خطایی در ارسال کد تایید دیجی کالا رخ داده است!"
                    };
                }
            }
            #endregion


            //Update Commisions
            var client = new HttpClient();
            var allCommissions = new List<CommissionItem>();
            int page = 1;
            int totalPages;

            do
            {
                var requestUrl = $"https://seller.digikala.com/api/v2/commissions?page={page}&size=200&search%5Bmy_commissions%5D=1";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("Cookie", $"seller_api_access_token={loginToken.access_token}");

                var response = await client.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _db.DigikalaLoginToken.Remove(loginToken);
                    _db.SaveChanges();
                    await UpdateCommisionAsync();
                }
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                var commissionData = JsonConvert.DeserializeObject<CommisionDataDtos>(responseContent);

                allCommissions.AddRange(commissionData.Data.Items);

                totalPages = commissionData.Data.Pager.total_pages;
                page++;
            } while (page <= totalPages);

            List<Commission> commissions = allCommissions.Select(s => new Commission
            {
                CategoryId = s.category_id,
                CommissionPercent = s.Commissions,
                Name = s.category_title
            }).ToList();

            _db.RemoveRange(_db.Commissions);
            await _db.AddRangeAsync(commissions);
            try
            {
                await _db.SaveChangesAsync();
                return new CommissionStatus
                {
                    Commission = commissions.Count,
                    Result = true
                };
            }
            catch (Exception)
            {
                return new CommissionStatus
                {
                    Result = false,
                    Message = "خطایی در ذخیره سازی کمیسیون ها پیش آمده است!"
                };
            }
        }

        #region Login
        public async Task<DigikalaLoginToken> Login(string UserName, string verificationCode)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://seller.digikala.com/api/v2/otp/verify");

            var json = JsonConvert.SerializeObject(new { otp = verificationCode, username = UserName });
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            try
            {
                var result = JsonConvert.DeserializeObject<DigikalaLoginResponse>(responseContent);
                return new DigikalaLoginToken
                {
                    access_token = result.Data.token.access_token,
                    expire_in = result.Data.token.expire_in,
                    refresh_token = result.Data.token.refresh_token
                };
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                return null;
            }

        }

        public async Task<bool> SendActivtionCode(string UserName)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://seller.digikala.com/api/v2/otp/send");

            var json = JsonConvert.SerializeObject(new { username = UserName });
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<DigiKalaActivationCodeResponse>(responseContent);
            if (responseData?.Status == "ok" && responseData.Data?.Message != null)
                return true;
            else
                return false;
        }

        public async Task<string?> GetVerificationCodeAsync(SetupSettings setting)
        {
            try
            {
                var client = new ImapClient();
                client.Connect(setting.EmailReceiverSetting.emailHost, setting.EmailReceiverSetting.emailPort, setting.EmailReceiverSetting.useSsl);
                client.Authenticate(setting.EmailReceiverSetting.emailReceiverUserName, setting.EmailReceiverSetting.emailReceiverpassword);
                client.Inbox.Open(FolderAccess.ReadOnly);

                //var messages = client.Inbox.Fetch(0, -1, MessageSummaryItems.UniqueId);
                var query = SearchQuery.FromContains(setting.DigikalaLoginSetting.getDataFromEmail);
                var uniqueIds = client.Inbox.Search(query);

                var message = uniqueIds.Reverse().FirstOrDefault();
                MimeMessage email = client.Inbox.GetMessage(message);

                string? verificationCode = await ExtractVerificationCodeAsync(email, setting.DigikalaLoginSetting.getDataFromEmail);
                client.Disconnect(true);
                return verificationCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private async Task<string?> ExtractVerificationCodeAsync(MimeMessage email, string? senderEmail)
        {
            string pattern = @"\d{6}";
            Match match;
            if (senderEmail != null && !email.From.Mailboxes.Any(mbox => mbox.Address == senderEmail))
            {
                return null;
            }
            match = Regex.Match(email.TextBody, pattern);
            return match.Success ? match.Value : null;
        }
        #endregion


        public async Task<DigikalaLoginToken?> GetDigikalaLoginTokenAsync()
        {
            try
            {
                var loginToken = await _db.DigikalaLoginToken.FirstOrDefaultAsync();

                return loginToken;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        #region Rate Limit
        public async Task<DigikalaLimitLog?> GetLastDigikalaLimitLogsAsync()
        {
            return await _db.DigikalaLimitLogs.OrderByDescending(o=>o.CreateDateTime).LastOrDefaultAsync(c => !c.Limited);
        }

        public async Task<bool> AddDigikalaLimitLogsAsync()
        {
            await _db.DigikalaLimitLogs.AddAsync(new DigikalaLimitLog
            {
                CreateDateTime = DateTime.Now,
                Limited = false,
                RequestCount = 1
            });

            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<(bool isSuccessful, bool isLimited)> AddNewRequestToDigikalaLimit()
        {
            var lastDigikalaLimitLog = await GetLastDigikalaLimitLogsAsync();
            if (lastDigikalaLimitLog != null)
            {
                var limitSetting = _setupSettings.DigiKalaRateLimitSetting;
                if (limitSetting.ManualRequestLimitCount > lastDigikalaLimitLog.RequestCount)
                {
                    lastDigikalaLimitLog.RequestCount++;
                    if (limitSetting.ManualRequestLimitCount == lastDigikalaLimitLog.RequestCount)
                    {
                        lastDigikalaLimitLog.Limited = true;
                    }
                }
                else
                {
                    lastDigikalaLimitLog.Limited = true;
                }

                try
                {
                    _db.Update(lastDigikalaLimitLog);
                    _db.SaveChanges();

                    return (true, lastDigikalaLimitLog.Limited);
                }
                catch (Exception)
                {
                    return (false, false);
                }
            }
            else
            {
                var status = await AddDigikalaLimitLogsAsync();
                return (status, false);
            }
        }

        public async Task<bool> RateLimit(HttpResponseMessage responseMessage)
        {
            var limitSetting = _setupSettings.DigiKalaRateLimitSetting;

            if (limitSetting.IsAutomate)
            {
                if (limitSetting.AutomateMinutesCoolDown != null && responseMessage.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var path = Utilities.GetSetupFilePath("DigikalaRateLimitLog.txt");
                    File.AppendAllText(path, $"[!] AutoMateRateLimit: Too Many Request At {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n");

                    File.AppendAllText(path, $"[+] AutoMateRateLimit: Start Automate Rate Limit CoolDown At {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} with CoolDown {limitSetting.AutomateMinutesCoolDown.Value}\n");
                    Thread.Sleep(TimeSpan.FromMinutes(limitSetting.AutomateMinutesCoolDown.Value));
                    File.AppendAllText(path, $"[-] AutoMateRateLimit: End Automate Rate Limit CoolDown At {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n");
                    return true;
                }
            }
            else if (limitSetting.ManualRequestLimitCount != null && limitSetting.ManualMinutesCoolDown != null)
            {
                var limitRequestStatus = await AddNewRequestToDigikalaLimit();

                if (limitRequestStatus.isSuccessful && limitRequestStatus.isLimited)
                {
                    var path = Utilities.GetSetupFilePath("DigikalaRateLimitLog.txt");
                    File.AppendAllText(path, $"[+] Manual Cooldown : The Request Count is reach the manual request limit count {limitSetting.ManualRequestLimitCount.Value} the cooldown minutes is {limitSetting.ManualMinutesCoolDown.Value} CreateTime : {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n");
                    Thread.Sleep(TimeSpan.FromMinutes(limitSetting.ManualMinutesCoolDown.Value));
                    File.AppendAllText(path, $"[-] Manual Cooldown : manual coolDown is ended at {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n");
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
