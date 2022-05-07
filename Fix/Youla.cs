using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
            public string City { get; set; } = string.Empty;
            public int PriceFrom { get; set; }
            public int PriceTo { get; set; }
            public string OwnerId { get; set; }
            public string Category { get; set; }
            public string Subcategory { get; set; }
            public int Limit { get; set; } = 100;
            public int Page { get; set; }

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


        public static async Task<IEnumerable<Product>> GetProducts(SearchParams searchParams)
        {

            Uri uri = new Uri("https://api.youla.io/api/v1/products");
            UriBuilder uriBuilder = new UriBuilder(uri);
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            City city = await GetCityBySlug(searchParams.City);


            query["city"] = city?.Id;
            query["category"] = searchParams.Category;
            query["subcategory"] = searchParams.Subcategory;
            query["limit"] = searchParams.Limit.ToString();
            query["page"] = searchParams.Page.ToString();
            query["price_from"] = searchParams.PriceFrom.ToString();
            query["price_to"] = searchParams.PriceTo.ToString();
            query["owner_id"] = searchParams.OwnerId;
            uriBuilder.Query = query.ToString();

            Uri requestUri = new Uri(uriBuilder.Uri.AbsoluteUri);
            HttpClient client = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
            JObject json = await JObject.LoadAsync(reader);
            ProductsResponse productsResponse = JsonConvert.DeserializeObject<ProductsResponse>(json.ToString());
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


        public static async Task<User> GetUserByIdAsync(string userId)
        {
            Uri requestUri = new Uri($"https://api.youla.io/api/v1/user/{userId}");
            HttpClient client = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
            JObject json = await JObject.LoadAsync(reader);
            UserResponse productsResponse = JsonConvert.DeserializeObject<UserResponse>(json.ToString());
            return productsResponse.User;
        }


        public static async IAsyncEnumerable<Product> GetAllProducts(SearchParams searchParams)
        {
            int i = 0;
            while (true)
            {
                bool isEmpty = true;
                searchParams.Page = i;
                IEnumerable<Product> products = await GetProducts(searchParams);
                foreach (Product product in products)
                {
                    isEmpty = false;
                    User user = await GetUserByIdAsync(product.Owner.id);
                    if(user != null)
                    {
                        product.Owner = user;
                    }
                    
                    product.Owner.id = product.Owner.id;
                    yield return product;
                }
                if (isEmpty) break;
                i++;
            }
        }
    }
}
