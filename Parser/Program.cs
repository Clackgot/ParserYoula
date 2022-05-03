using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Parser
{

    public static class YoulaApi
    {
        public static async Task<Product> Product(string id)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.youla.io/api/v1/product/{id}"));
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
            JObject json = await JObject.LoadAsync(reader);
            return JsonConvert.DeserializeObject<Product>(json.ToString());
        }

        public static async Task<User> User(string id)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.youla.io/api/v1/user/{id}"));
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));
            JObject json = await JObject.LoadAsync(reader);
            return JsonConvert.DeserializeObject<User>(json.ToString());
        }

    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Product product = await YoulaApi.Product("620f3cd4fd03ff2af72e98e9");
                Console.WriteLine(product);
                User user = await YoulaApi.User("59e981a2cf204580004ca894");
                Console.WriteLine(user);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        

    }
}
