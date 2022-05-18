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
                //Console.ForegroundColor = ConsoleColor.Green;
                //Product product = await YoulaApi.Product("61e7f29fd069ef75944c1693");
                //Console.WriteLine(product);
                //Console.ResetColor();
                User user = await YoulaApi.User("5a03237180e08e05465886a4");
                Console.WriteLine(user.Data.Type);


            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        

    }
}
