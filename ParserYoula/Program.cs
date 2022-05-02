﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParserYoula
{
    public struct SearchAttributes
    {
        public string categorySlug { get; set; }
        public string subcategorySlug { get; set; }
        public string locationId { get; set; }

        public int? priceFrom { get; set; }
        public int? priceTo { get; set; }
        public override string ToString()
        {
            return $"{categorySlug} {subcategorySlug} {locationId} {priceFrom} - {priceTo}";
        }

    }
    public struct Product
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public int? Price { get; set; }
        public int? MarksCount { get; set; }

        public override string ToString()
        {
            return $"{Id}\t {OwnerId}\t {Name}\t {Price / 100} руб.\t {MarksCount}";
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
            //Console.WriteLine("Link:");
            string link = @"https://youla.ru/novoorsk/zhivotnye/gryzuny?attributes[price][from]=50000";
            SearchAttributes result = await ParseParamsFromLink(link);
            //Console.WriteLine($"[{result.categorySlug}][{result.subcategorySlug}]");
            //Console.WriteLine($"{result.priceFrom}");
            Console.WriteLine("ID\t OwnerID\t Name\t Price\t Marks\t");
            await foreach (var item in GetProducts(result))
            {
                Console.WriteLine(item);
            }

            //var searchAttributes = await ParseSearchParams("https://youla.ru/novoorsk/zhivotnye/gryzuny");
            //var searchAttributes = await ParseParamsFromLink("https://youla.ru/novoorsk/zhivotnye/gryzuny?attributes[price][to]=570000&attributes[price][from]=335000");
            //Console.WriteLine(searchAttributes);
        }


        /// <summary>
        /// Получает параметры поискового запроса из ссылки
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public async Task<SearchAttributes> ParseParamsFromLink(string link)
        {
            #region Получение HTML содержимого
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
        new Uri(link));
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9");
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("Origin", "https://youla.ru");
            request.Headers.Add("Referer", link);
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "same-origin");
            request.Headers.Add("Sec-Fetch-User", "?1");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36");
            request.Headers.Add("accept", "*/*");
            request.Headers.Add("appId", "web/3");
            //request.Headers.Add("authorization", "");
            request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"100\", \"Google Chrome\";v=\"100\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            HttpResponseMessage result = client.SendAsync(request).Result;
            string document = await result.Content.ReadAsStringAsync();
            #endregion

            #region Получение Json содержимого из HTML
            Regex regex = new Regex(@"window.__YOULA_STATE__ = (.*?);$", RegexOptions.Multiline | RegexOptions.Multiline);

            Match match = regex.Match(document);
            string jsonText = match.Groups[1].Value;
            #endregion

            #region Декодирование Юникода
            var jsonConverter = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonText);

            byte[] bytes = Encoding.Default.GetBytes(jsonConverter.ToString());
            jsonText = Encoding.UTF8.GetString(bytes);
            #endregion

            #region Парсинг атрибутов из JSON
            SearchAttributes searchAttributes = new SearchAttributes();
            JObject json = JObject.Parse(jsonText);
            try { searchAttributes.categorySlug = json["data"]["routeParams"]["categorySlug"].ToString(); } catch { };
            try { searchAttributes.subcategorySlug = json["data"]["routeParams"]["subcategorySlug"].ToString(); } catch { };
            try { searchAttributes.locationId = json["entities"]["cities"][0]["id"].ToString(); } catch { };
            try { searchAttributes.priceFrom = int.Parse(json["data"]["requestParams"]["attributes"]["price"]["from"].ToString()) / 100; } catch { };
            try { searchAttributes.priceTo = int.Parse(json["data"]["requestParams"]["attributes"]["price"]["to"].ToString()) / 100; } catch { };
            #endregion

            return searchAttributes;
        }
        /// <summary>
        /// Возвращает объявления с N-ой страницы
        /// </summary>
        /// <param name="priceFrom"></param>
        /// <param name="priceTo"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<Product> GetProducts(SearchAttributes searchAttributes)
        {

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
        new Uri("https://api-gw.youla.io/federation/graphql"));

            #region Заголовки запроса
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9");
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("Origin", "https://youla.ru");
            request.Headers.Add("Referer", "https://youla.ru");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "cross-site");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36");
            request.Headers.Add("accept", "*/*");
            request.Headers.Add("appId", "web/3");
            request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"100\", \"Google Chrome\";v=\"100\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("uid", "626f35c0911f7");
            request.Headers.Add("x-app-id", "web/3");
            request.Headers.Add("x-uid", "626f35c0911f7");
            request.Headers.Add("x-youla-splits", "8a=8|8b=1|8c=0|8m=0|8v=0|8z=0|16a=0|16b=0|64a=4|64b=0|100a=35|100b=9|100c=0|100d=0|100m=0");

            #endregion


            #region Тело запроса
            string requestBodyJsonText = @"{""operationName"":""catalogProductsBoard"",""variables"":{""sort"":""DEFAULT"",""attributes"":[{""slug"":""price"",""value"":null,""from"":123400,""to"":567800},{""slug"":""categories"",""value"":[""zhivotnye""],""from"":null,""to"":null}],""datePublished"":null,""location"":{""latitude"":null,""longitude"":null,""city"":""576d0617d53f3d80945f9428"",""distanceMax"":null},""search"":"""",""cursor"":""""},""extensions"":{""persistedQuery"":{""version"":1,""sha256Hash"":""bf7a22ef077a537ba99d2fb892ccc0da895c8454ed70358c0c7a18f67c84517f""}}}";
            JObject requestBodyJson = JObject.Parse(requestBodyJsonText);
            try
            {
                requestBodyJson["variables"]["attributes"][0]["from"] = searchAttributes.priceFrom * 100;
                requestBodyJson["variables"]["attributes"][0]["to"] = searchAttributes.priceTo * 100;
            }
            catch
            {
                Console.WriteLine("Не удалось задать цену запроса");
            }

            try
            {
                if (!string.IsNullOrEmpty(searchAttributes.subcategorySlug))
                {
                    //requestBodyJson["variables"]["attributes"][1]["value"] = "1000";
                    requestBodyJson["variables"]["attributes"][1]["value"][0] = searchAttributes.subcategorySlug;
                }
                else
                {
                    requestBodyJson["variables"]["attributes"][1]["value"][0] = searchAttributes.categorySlug;
                }
            }
            catch
            {
                Console.WriteLine("Не удалось задать категории");
            }



            try
            {
                requestBodyJson["variables"]["location"]["city"] = searchAttributes.locationId;
            }
            catch
            {
                Console.WriteLine("Не удалось задать город запроса");
            }


            StringContent content = new StringContent(requestBodyJson.ToString(), Encoding.UTF8, "application/json");
            request.Content = content;
            #endregion


            #region Ответ на запрос
            HttpResponseMessage result = client.SendAsync(request).Result;
            string jsonResponseText = await result.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(jsonResponseText);
            #endregion

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
                try
                {
                    string name = item["product"]["name"].ToString();
                    product.Name = name;
                }
                catch
                {
                    isCorrect = false;
                }
                try
                {
                    string realPrice = item["product"]["price"]["realPrice"]["price"].ToString();
                    int price;
                    int.TryParse(realPrice, out price);
                    product.Price = price;
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
