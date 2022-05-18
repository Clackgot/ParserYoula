using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Fix
{

    public abstract class JsonEntity
    {
        public override string ToString()
        {
            return JObject.Parse(JsonConvert.SerializeObject(this)).ToString();
        }
    }

    public static class Yola
    {
        public class SearchParams : JsonEntity
        {
            public string CityId { get; set; }
            public string CitySlug { get; set; } = string.Empty;
            public int? PriceFrom { get; set; }
            public int? PriceTo { get; set; }
            public string OwnerId { get; set; }
            public string Category { get; set; }
            public string Subcategory { get; set; }
            public int Limit { get; set; } = 100;
            public int Page { get; set; } = 0;

            public async Task<Uri> GetUri()
            {
                Uri uri = new Uri("https://api.youla.io/api/v1/products");
                UriBuilder uriBuilder = new UriBuilder(uri);
                NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
                City city = await GetCityBySlug(CitySlug);

                CityId = city?.Id;
                if (city != null) query["city"] = CityId;
                if (Category != null) query["category"] = Category;
                if (Subcategory != null) query["subcategory"] = Subcategory;
                if (Limit != 0) query["limit"] = Limit.ToString();
                if (Page >= 0) query["page"] = Page.ToString();
                if (PriceFrom != null && PriceFrom >= 0) query["attributes[price][from]"] = PriceFrom.ToString();
                if (PriceTo != null && PriceTo > 0) query["attributes[price][to]"] = PriceTo.ToString();
                if (OwnerId != null) query["owner_id"] = OwnerId;

                uriBuilder.Query = query.ToString();
                return uriBuilder.Uri;
            }


            public SearchParams(string link)
            {
                Uri uri = new Uri(link);
                NameValueCollection queryParams = HttpUtility.ParseQueryString(uri.Query);
                IEnumerable<string> segments = uri.Segments.Select(s => s.Trim('/')).Where(s => !string.IsNullOrWhiteSpace(s));
                #region Query атрибуты
                IEnumerable<string> categories = segments.Skip(1);

                string category = categories.FirstOrDefault();
                string subcategory = categories.LastOrDefault();
                string city = segments.FirstOrDefault();
                string priceToParam = queryParams.Get("attributes[price][to]");
                string priceFromParam = queryParams.Get("attributes[price][from]");
                #endregion
                CitySlug = city;
                Category = category;
                Subcategory = subcategory;
                if (int.TryParse(priceToParam, out int priceTo))
                {
                    PriceTo = priceTo;
                }
                if (int.TryParse(priceFromParam, out int priceFrom))
                {
                    PriceFrom = priceFrom;
                }

            }
            public SearchParams()
            {

            }

        }

        public class ProductsResponse : JsonEntity
        {
            [JsonProperty("data")]
            public Product[] Products { get; set; } = Array.Empty<Product>();
            [JsonProperty("status")]
            public int Status { get; set; }
            [JsonProperty("detail")]
            public string Detail { get; set; }
            [JsonProperty("uri")]
            public string Uri { get; set; }
            [JsonProperty("meta")]
            public Meta Meta { get; set; }
        }

        public class Meta : JsonEntity
        {
            [JsonProperty("search_id")]
            public string SearchId { get; set; }
            [JsonProperty("serp_id")]
            public string SerpId { get; set; }
            [JsonProperty("engine")]
            public int Engine { get; set; }
            [JsonProperty("sid")]
            public string Sid { get; set; }
        }

        public static async Task<Product> GetProduct(string id)
        {
            try
            {
                Uri requestUri = new Uri($"https://api.youla.io/api/v1/product/{id}");
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                HttpResponseMessage response = await client.SendAsync(httpRequest);
                Stream contentStream = await response.Content.ReadAsStreamAsync();
                JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
                JObject json = await JObject.LoadAsync(reader);
                JToken data = json["data"];
                Product product = JsonConvert.DeserializeObject<Product>(data.ToString());
                if (product.Owner.settings.CallSettings == null) throw new Exception();
                return product;
            }
            catch
            {
                throw new Exception();
            }
        }
        public static async Task<IEnumerable<Product>> GetProducts(SearchParams searchParams)
        {
            Uri requestUri = await searchParams.GetUri();
            HttpClient client = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
            JObject json = await JObject.LoadAsync(reader);
            ProductsResponse productsResponse = JsonConvert.DeserializeObject<ProductsResponse>(json.ToString());
            int count = productsResponse.Products.Length;
            int current = 1;
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 15;
            
            Parallel.ForEach(productsResponse.Products, parallelOptions, product => 
            {

                Console.WriteLine($"[{current}|{count}] {product.Name}");
                int errorsCount = 0;
                while (true)
                {
                    try
                    {
                        product.Owner = GetUserByIdAsync(product.Owner.idString).Result;
                        //product.Owner.settings = GetProduct(product.IdString).Result.Owner.settings;
                        Product productResult = GetProduct(product.IdString).Result;
                        product.Owner.settings = productResult.Owner.settings;
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        errorsCount++;
                        if (parallelOptions.MaxDegreeOfParallelism > 2 && errorsCount > 5)
                        {
                            int threads = parallelOptions.MaxDegreeOfParallelism;

                            parallelOptions.MaxDegreeOfParallelism--;
                            int threadsCurrent = parallelOptions.MaxDegreeOfParallelism;
                            
                            Console.WriteLine($"Уменьшено кол-во потоков с {threads} до {threadsCurrent}");
                            TimeSpan delay = TimeSpan.FromSeconds(3);
                            Console.WriteLine($"Ждём {delay.TotalSeconds} секунд");
                            Thread.Sleep(delay);
                            
                        }
                        else
                        {
                            TimeSpan delay = TimeSpan.FromSeconds(2);
                            Console.WriteLine($"Ждём {delay.TotalSeconds} секунд");
                            Thread.Sleep(delay);
                        }
                        Console.ResetColor();
                    }
                    break;
                }
                current++;
            });
            //foreach (var product in productsResponse.Products)
            //{
            //    Console.WriteLine($"[{current}|{count}] {product.Name}");
            //    product.Owner = await GetUserByIdAsync(product.Owner.idString);
            //    product.Owner.settings = (await GetProduct(product.IdString)).Owner.settings;
            //    current++;
            //}
            return productsResponse.Products;
        }

        private static async Task<City> GetCityBySlug(string city)
        {
            Uri requestUri = new Uri("https://api.youla.io/api/v1/geo/cities");
            HttpClient client = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
            JObject json = await JObject.LoadAsync(reader);
            CitiesResponse productsResponse = JsonConvert.DeserializeObject<CitiesResponse>(json.ToString());
            return productsResponse.Citites.FirstOrDefault(c => c.Slug == city);
        }
        private static async Task<City> GetCitySlugById(string id)
        {
            Uri requestUri = new Uri("https://api.youla.io/api/v1/geo/cities");
            HttpClient client = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
            JObject json = await JObject.LoadAsync(reader);
            CitiesResponse productsResponse = JsonConvert.DeserializeObject<CitiesResponse>(json.ToString());
            return productsResponse.Citites.FirstOrDefault(c => c.Id == id);
        }


        public static async Task<User> GetUserByIdAsync(string userId)
        {
            Uri requestUri = new Uri($"https://api.youla.io/api/v1/user/{userId}");
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
            JObject json = await JObject.LoadAsync(reader);
            UserResponse productsResponse = JsonConvert.DeserializeObject<UserResponse>(json.ToString());
            return productsResponse.User;
        }


        public static async Task<IEnumerable<Product>> GetAllProducts(SearchParams searchParams)
        {
            List<Product> allProducts = new List<Product>();
            int i = 0;
            while (true)
            {
                searchParams.Page = i;
                IEnumerable<Product> products = await GetProducts(searchParams);
                allProducts.AddRange(products);
                if (products.Count() == 0) break;
                i++;
            }
            return allProducts;
        }
    }
}
