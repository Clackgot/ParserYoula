using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ParserYoula
{
    public struct SearchAttributes
    {
        public string categories { get; set; }
        public string subcategories { get; set; }
        public string location { get; set; }
    }
    public struct Product
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public int MarksCount { get; set; }

        public override string ToString()
        {
            return $"{Id} {OwnerId} {Name} {MarksCount}";
        }
    }
    class Parser
    {
        private HttpClient client = new HttpClient();
        public Parser()
        {

        }

        public async Task Run()
        {
            await foreach (var item in GetProducts(2000, 5000, 1))
            {
                Console.WriteLine(item);
            }
        }


        /// <summary>
        /// Возвращает объявления с N-ой страницы
        /// </summary>
        /// <param name="priceFrom"></param>
        /// <param name="priceTo"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<Product> GetProducts(int priceFrom, int priceTo, int pageNumber)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
                new Uri("https://api-gw.youla.io/federation/graphql"));
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9");
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("Origin", "https://youla.ru");
            request.Headers.Add("Referer", "https://youla.ru/rostov-na-donu/zhivotnye/tovary?attributes%5Bprice%5D%5Bfrom%5D=200000&attributes%5Bsort_field%5D=date_published");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "cross-site");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36");
            request.Headers.Add("accept", "*/*");
            request.Headers.Add("appId", "web/3");
            //request.Headers.Add("authorization", "");
            request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"100\", \"Google Chrome\";v=\"100\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("uid", "626f35c0911f7");
            request.Headers.Add("x-app-id", "web/3");
            request.Headers.Add("x-uid", "626f35c0911f7");
            request.Headers.Add("x-youla-splits", "8a=8|8b=1|8c=0|8m=0|8v=0|8z=0|16a=0|16b=0|64a=4|64b=0|100a=35|100b=9|100c=0|100d=0|100m=0");
            string requestBodyJson = @"{""operationName"":""catalogProductsBoard"",""variables"":{""sort"":""DATE_PUBLISHED_DESC"",""attributes"":[{""slug"":""price"",""value"":null,""from"":200000,""to"":null},{""slug"":""sort_field"",""value"":null,""from"":null,""to"":null},{""slug"":""categories"",""value"":[""tovary""],""from"":null,""to"":null}],""datePublished"":null,""location"":{""latitude"":null,""longitude"":null,""city"":""576d0617d53f3d80945f952c"",""distanceMax"":null},""search"":"""",""cursor"":""""},""extensions"":{""persistedQuery"":{""version"":1,""sha256Hash"":""bf7a22ef077a537ba99d2fb892ccc0da895c8454ed70358c0c7a18f67c84517f""}}}";


            StringContent content = new StringContent(JObject.Parse(requestBodyJson).ToString(), Encoding.UTF8, "application/json");
            request.Content = content;
            HttpResponseMessage result = client.SendAsync(request).Result;
            string jsonResponseText = await result.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(jsonResponseText);
            List<Product> products = new List<Product>();
            foreach (JToken item in jsonResponse["data"]["feed"]["items"])
            {
                Product product = new Product();
                bool isCorrect = true;
                try
                {
                    string id = item["product"]["id"].ToString();
                    product.Id = id;
                }
                catch
                {
                    isCorrect = false;
                }
                if (isCorrect) yield return product;

            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Parser parser = new Parser();
            parser.Run().Wait();

        }

    }
}
