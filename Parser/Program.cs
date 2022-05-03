using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Parser
{



    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await Get();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        
        private static async Task Get()
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("https://api.youla.io/api/v1/product/62714543ee67b9067107a583"));
            HttpResponseMessage response = await client.SendAsync(httpRequest);
            //byte[] content = await response.Content.ReadAsByteArrayAsync();
            //string contentString = await response.Content.ReadAsStringAsync();
            Stream contentStream = await response.Content.ReadAsStreamAsync();
            //JObject.Parse(contentString);

            //string jsonText = Encoding.UTF8.GetString(content);

            JsonTextReader reader = new JsonTextReader(new StreamReader(contentStream));

            JObject json = await JObject.LoadAsync(reader);

            Product product = JsonConvert.DeserializeObject<Product>(json.ToString());

            Console.WriteLine(product.data.DateCreated);
        }
    }
}
