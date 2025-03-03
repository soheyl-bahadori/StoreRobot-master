using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoreRobot.Models;

namespace StoreRobot.Utils
{
    public class WooCommerceApi
    {
        private string _apiDomain;
        private string _apiAuth;
        private StoreEnum _store;

        public WooCommerceApi(StoreEnum store)
        {
            _store = store;
            _apiDomain = store == StoreEnum.Pakhsh ? "https://pakhsh.shop" : "https://safirkala.com";

            var settings = Utilities.GetSettings();

            _apiAuth = store == StoreEnum.Pakhsh
                ? settings.ApiKeys.Pakhsh
                : settings.ApiKeys.SafirKala;
            //_apiAuth = store == StoreEnum.Pakhsh
            //    ? "ck_6c5514e842ecca22e1c9e24a6a6249630bd6b1fd:cs_ebe4b00ec4824d9824b00f1b2a925e032a4d82b9"
            //    : "ck_4c694acccae12a8bd96503a950c710828e290a48:cs_9b967b5ab3549030a8cebc6b41370e9ed3fcce4f";
        }
        public async Task<bool> ServerRunningAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                using var request = new HttpRequestMessage(new HttpMethod("GET"),
                    $"{_apiDomain}/wp-json/wc/v3/products?sku=R1324555555");
                var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

                var response = await httpClient.SendAsync(request).ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetProductAsync(List<string> skus)
        {
            while (true)
            {
                try
                {
                    string sku = string.Join(',', skus);
                    using var httpClient = new HttpClient();
                    using var request = new HttpRequestMessage(new HttpMethod("GET"), $"{_apiDomain}/wp-json/wc/v3/products?sku={sku}&per_page=100");
                    var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
                    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

                    var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode) return null;


                    using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    var result = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                    try
                    {
                        /*JArray jsonResult = (JArray)JsonConvert.DeserializeObject(result);

                        return JsonConvert.SerializeObject(jsonResult[0]);*/
                        _ = (JArray)JsonConvert.DeserializeObject(result);

                        return result;
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
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

        public async Task<string?> GetProductAsync(string parentId)
        {
            while (true)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    using var request = new HttpRequestMessage(new HttpMethod("GET"), $"{_apiDomain}/wp-json/wc/v3/products/{parentId}");
                    var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
                    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

                    var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode) return null;


                    using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    var result = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                    try
                    {
                        /*JArray jsonResult = (JArray)JsonConvert.DeserializeObject(result);

                        return JsonConvert.SerializeObject(jsonResult[0]);*/
                        _ = (JToken)JsonConvert.DeserializeObject(result);

                        return result;
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
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

        public async Task<Result> UpdateProductAsync(string id, int stockQuantity, string userPrice, bool isOffEnabled, string priceInOff, string attributes, bool isParent, string? parentId)
        {
            while (true)
            {
                try
                {
                    string url = parentId == null ?
                        $"{_apiDomain}/wp-json/wc/v3/products/{id}" : $"{_apiDomain}/wp-json/wc/v3/products/{parentId}/variations/{id}";
                    string finalOffPrice = isOffEnabled ? priceInOff : "";
                    using var httpClient = new HttpClient();
                    using var request = new HttpRequestMessage(new HttpMethod("PUT"), url);
                    var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
                    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

                    request.Content = !isParent ? new StringContent($"{{\n  \"stock_quantity\": \"{stockQuantity}\",\n\"regular_price\": \"{userPrice}\",\n\"sale_price\": \"{finalOffPrice}\"\n,\n\"attributes\": {attributes}\n}}")
                            : new StringContent($"{{\n  \"attributes\": {attributes}}}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(request);
                    using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    var result = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                        return new Result() { Status = true, Message = result };

                    return new Result() { Status = true };
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

        public async Task<string?> GetWebHooksAsync()
        {
            while (true)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    using var request = new HttpRequestMessage(new HttpMethod("GET"), $"{_apiDomain}/wp-json/wc/v3/webhooks?per_page=100");
                    var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
                    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

                    var response = await httpClient.SendAsync(request).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode) return null;

                    using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    var result = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                    try
                    {
                        _ = (JArray)JsonConvert.DeserializeObject(result);

                        return result;
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
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

        public async Task<bool> UpdateWebHookAsync(string id, string status)
        {
            while (true)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    using var request = new HttpRequestMessage(new HttpMethod("PUT"),
                        $"{_apiDomain}/wp-json/wc/v3/webhooks/{id}");
                    var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
                    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

                    request.Content = new StringContent($"{{\n  \"status\": \"{status}\"\n}}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(request);
                    return response.IsSuccessStatusCode;
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

    }
}
