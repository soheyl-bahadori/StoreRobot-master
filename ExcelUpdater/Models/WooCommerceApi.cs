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
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ExcelUpdater.Models;

namespace ExcelUpdater.Models
{
    public class WooCommerceApi
    {
        private string _apiDomain;
        private string _apiAuth;
        private StoreEnum _store;

        public WooCommerceApi(StoreEnum store)
        {
            _store = store;
            _apiDomain = store == StoreEnum.Pakhsh ? "https://pakhsh.shop" : "https://www.safirkala.com";
            var settings = Utilities.GetSettings();

            _apiAuth = store == StoreEnum.Pakhsh
                ? settings.ApiKeys.Pakhsh
                : settings.ApiKeys.SafirKala;
        }

        public async Task<string> GetProductAsync(List<string> skus)
        {
            string sku = string.Join(',', skus);
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("GET"), $"{_apiDomain}/wp-json/wc/v3/products?sku={sku}&per_page=100");
            var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
            request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);


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
                throw new Exception(e.ToString + "\n\n" + result); ;
            }

        }

        public async Task<string> GetProductAsync(string parentId)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("GET"), $"{_apiDomain}/wp-json/wc/v3/products/{parentId}");
            var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
            request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);


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
                throw new Exception(e.ToString + "\n\n" + result); ;
            }

        }

        public async Task<bool> UpdateProductAsync(string id, int stockQuantity, string userPrice, bool isOffEnabled, string priceInOff, string attributes, bool isParent, string? parentId)
        {
            //datas
            string url = parentId == null ?
                $"{_apiDomain}/wp-json/wc/v3/products/{id}" : $"{_apiDomain}/wp-json/wc/v3/products/{parentId}/variations/{id}";
            string finalOffPrice = isOffEnabled ? priceInOff : "";
            using var httpClient = new HttpClient();
            var base64Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiAuth}"));
            var content = !isParent ? $"{{\n  \"stock_quantity\": \"{stockQuantity}\",\n\"regular_price\": \"{userPrice}\",\n\"sale_price\": \"{finalOffPrice}\"\n,\n\"attributes\": {attributes}\n}}"
                : $"{{\n  \"attributes\": {attributes}}}";

            //build request
            using var request = new HttpRequestMessage(new HttpMethod("PUT"), url);
            request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");
            request.Content = new StringContent(content);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            //send request
            var response = await httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode && string.IsNullOrEmpty(result))
            {
                using var request2 = new HttpRequestMessage(new HttpMethod("PUT"), url);
                request2.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");
                request2.Content = new StringContent(content);
                request2.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                response = await httpClient.SendAsync(request2);
                result = await response.Content.ReadAsStringAsync();
            }
            return response.IsSuccessStatusCode;

        }
        private HttpRequestMessage BuildRequestMessage(string url, string base64Authorization, string content)
        {
            using var request = new HttpRequestMessage(new HttpMethod("PUT"), url);
            request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64Authorization}");
            return request;
        }
    }
}
