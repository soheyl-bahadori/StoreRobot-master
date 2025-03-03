using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using static System.Net.Mime.MediaTypeNames;

namespace WebHookReceiver.Models
{
    public class DigiKalaApi
    {
        private string _apiKey;
        private SetupSettings _setupSettings;
        public DigiKalaApi()
        {
            _setupSettings = Utilities.GetSettings();
            _apiKey = _setupSettings.ApiKeys.DigiKala;
        }

        public Result GetOrders(int page)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(500);
            using var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://seller.digikala.com/api/v1/orders/?page={page}/");
            request.Headers.TryAddWithoutValidation("Authorization", _apiKey);

            var response = httpClient.Send(request);
            using var streamReader = new StreamReader(response.Content.ReadAsStream());
            var result = streamReader.ReadToEnd();
            if (!response.IsSuccessStatusCode)
            {
                return new Result() { Status = false, Message = result };
            }

            var deserialize = (JObject)JsonConvert.DeserializeObject(result);

            return new Result() { Status = true, Message = JsonConvert.SerializeObject(deserialize["data"]["items"]) };

        }

        public Result GetOrdersPage()
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(500);
            using var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://seller.digikala.com/api/v1/orders/");
            request.Headers.TryAddWithoutValidation("Authorization", _apiKey);

            var response = httpClient.Send(request);
            using var streamReader = new StreamReader(response.Content.ReadAsStream());
            var result = streamReader.ReadToEnd();
            if (!response.IsSuccessStatusCode)
            {
                return new Result() { Status = false, Message = result };
            }

            var deserialize = (JObject)JsonConvert.DeserializeObject(result);

            return new Result() { Status = true, Message = (string)deserialize["data"]["pager"]["total_page"] };

        }

        public async Task<string> GetProductAsync(string dkpc)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(500);
            using var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://seller.digikala.com/api/v1/variants/?search[id]={dkpc}");
            request.Headers.TryAddWithoutValidation("Authorization", _apiKey);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var result = await streamReader.ReadToEndAsync().ConfigureAwait(false);

            var deserialize = (JObject)JsonConvert.DeserializeObject(result);

            if ((string)deserialize["status"] != "ok" || ((JArray)deserialize["data"]["items"]).Count == 0)
                throw new Exception($"در خواندن اطلاعات کالای dkpc-{dkpc} مشکلی به وجود آمده."); ;

            return JsonConvert.SerializeObject(deserialize["data"]["items"][0]);

        }

        public async Task<Result> UpdateProductAsync(string dkpc, int stockQuantity, bool isActive, int oldStock, bool oldIsActive, int? price = null)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(500);
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
                if (!response1.IsSuccessStatusCode)
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
                if (!response1.IsSuccessStatusCode)
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

            if (!response.IsSuccessStatusCode)
            {
                var resultObj = (JObject)JsonConvert.DeserializeObject(result);
                result = JsonConvert.SerializeObject(resultObj["data"]);


                return new Result()
                { Status = false, Message = result.Replace(@"""", "").Replace("{", "").Replace("}", "") };
            }
            return new Result() { Status = true, Message = result };

        }
    }
}
