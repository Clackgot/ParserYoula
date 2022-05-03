using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
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
        public string Description { get; set; }
        public int? Price { get; set; }
        public int? MarksCount { get; set; }

        public bool IsShop { get; set; }

        public override string ToString()
        {
            return $"{Id}\t {OwnerId}\t {Name}\t {Price / 100} руб.\t {MarksCount}";
        }
    }

    class ParserEventArgs : EventArgs
    {
        public List<Product> Products { get; set; }
        public SearchAttributes SearchAttributes;
    }
    class Parser
    {
        private HttpClient client = new HttpClient();
        private YoulaDataBase dataBase = new YoulaDataBase("data.db");

        public event EventHandler<ParserEventArgs> ParseCompleted;


        //private void Checker_CheckCompleted(object sender, ParserEventArgs e)
        //{
        //    Save(e.SearchAttributes, e.Products);
        //    Environment.Exit(0);
        //}


        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //Вызываем событие завершения проверки
            ParseCompleted.Invoke(this, new ParserEventArgs()
            {
                SearchAttributes = new SearchAttributes()
                {
                    categorySlug = searchAttrubutes.categorySlug,
                    subcategorySlug = searchAttrubutes.subcategorySlug,
                    priceFrom = searchAttrubutes.priceFrom,
                    priceTo = searchAttrubutes.priceTo,
                    locationId = searchAttrubutes.locationId
                },
                Products = products
            });
        }


        public Parser()
        {
            ParseCompleted += Parser_ParseCompleted;
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Parser_ParseCompleted(object sender, ParserEventArgs e)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Сохранение. Ждите...");
            Console.ForegroundColor = ConsoleColor.Gray;
            Save(searchAttrubutes, products);
            Environment.Exit(0);
        }

        private SearchAttributes searchAttrubutes;
        private List<Product> products = new List<Product>();

        public async Task Run()
        {

            Console.WriteLine("Ссылка:");
            string link = Console.ReadLine();

            //string link = @"https://youla.ru/novoorsk/zhivotnye/gryzuny?attributes[price][from]=50000";
            //string link = @"https://youla.ru/novoorsk/zhivotnye?attributes[price][to]=100000000&attributes[price][from]=100";
            //string link = @"https://youla.ru/novoorsk/zhivotnye/koshki";

            while (true)
            {
                try
                {
                    searchAttrubutes = await ParseParamsFromLink(link);
                }
                catch
                {
                    Console.WriteLine("Не удалось получить информацию из ссылки. Повторная попытка через 5 секунд");
                    await Task.Delay(5000);
                    continue;
                }
                break;
            }
            await foreach (var product in GetAllProducts(searchAttrubutes))
            {
                products.Add(product);
                Console.WriteLine($"{product.IsShop}");
            }

            //if (!string.IsNullOrEmpty(result.subcategorySlug))
            //{
            //    Save(products, result.subcategorySlug);
            //}
            //else
            //{
            //    Save(products, result.categorySlug);
            //}


            Save(searchAttrubutes, products);

        }

        private List<Product> SaveToDatabase(List<Product> products)
        {
            dataBase.Create();
            List<Product> addedProducts = dataBase.AddProducts(products);
            return addedProducts;
        }

        private void Save(SearchAttributes result, List<Product> products)
        {
            try
            {
                var addedProducts = SaveToDatabase(products);

                if (!string.IsNullOrEmpty(result.subcategorySlug))
                {
                    SaveToExcel(addedProducts, result.subcategorySlug);
                }
                else
                {
                    SaveToExcel(addedProducts, result.categorySlug);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка сохранения.");
                Console.WriteLine(e.Message);
                return;
            }
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Сохранено!");
            Console.ForegroundColor = ConsoleColor.Gray;

        }

        private static void SaveToExcel(List<Product> products, string listName)
        {
            var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add(listName);
            sheet.Cells[1, 1].Value = "Ссылка";
            sheet.Cells[1, 2].Value = "Название";
            sheet.Cells[1, 3].Value = "Описание";

            int row = 2;
            int col = 1;

            foreach (var product in products)
            {
                sheet.Cells[row, col].Value = $"https://youla.ru/p{product.Id}";
                sheet.Cells[row, col + 1].Value = product.Name;
                sheet.Cells[row, col + 2].Value = product.Description.Trim();
                row++;
            }

            sheet.Protection.IsProtected = false;
            var excel = package.GetAsByteArray();
            File.WriteAllBytes("result.xlsx", excel);
        }


        public async IAsyncEnumerable<Product> GetAllProducts(SearchAttributes searchAttributes)
        {
            int page = 0;
            bool isEmpty = false;
            while (!isEmpty)
            {
                isEmpty = true;
                IAsyncEnumerable<Product> products = GetProducts(searchAttributes, page);
                await foreach (Product item in products)
                {
                    yield return await GetProductInfo(item);
                    isEmpty = false;
                }
                page++;
            }
        }


        public async Task<Product> GetProductInfo(Product product)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
        new Uri($"https://api.youla.io/api/v1/product/{product.Id}"));

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
            #endregion


            #region Ответ на запрос
            HttpResponseMessage result = client.SendAsync(request).Result;
            string jsonResponseText = await result.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(jsonResponseText);
            try
            {
                product.OwnerId = jsonResponse["data"]["owner"]["id"].ToString();
                int marksCount;
                bool isShop;
                if (int.TryParse(jsonResponse["data"]["owner"]["rating_mark_cnt"].ToString(), out marksCount))
                {
                    product.MarksCount = marksCount;
                }
                else
                {
                    product.MarksCount = null;
                }
                product.Description = jsonResponse["data"]["description"].ToString();
                Console.WriteLine($"[{jsonResponse["data"]["owner"]["isShop"]}]");
            }
            catch { };
            #endregion


            return product;
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
            try
            {
                var jsonConverter = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonText);
                byte[] bytes = Encoding.Default.GetBytes(jsonConverter.ToString());
                jsonText = Encoding.UTF8.GetString(bytes);
            }
            catch
            {

            }
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
        public async IAsyncEnumerable<Product> GetProducts(SearchAttributes searchAttributes, int pageNumber)
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


            #region Номер страницы
            try
            {
                if (pageNumber <= 0)
                {
                    requestBodyJson["variables"]["cursor"] = "";
                }
                else
                {
                    var cursor = "{\"page\":0,\"totalProductsCount\":30,\"totalPremiumProductsCount\":0,\"dateUpdatedTo\":1651519522}";
                    var cursorJson = JObject.Parse(cursor);
                    cursorJson["page"] = pageNumber - 1;
                    //Console.WriteLine(cursorJson);
                    requestBodyJson["variables"]["cursor"] = cursorJson.ToString();
                }
            }
            catch
            {
                Console.WriteLine("Не удалось задать страницу");
            }
            #endregion


            #region Ценовой диапазон
            try
            {

                requestBodyJson["variables"]["attributes"][0]["from"] = searchAttributes.priceFrom * 100;
                requestBodyJson["variables"]["attributes"][0]["to"] = searchAttributes.priceTo * 100;
            }
            catch
            {
                Console.WriteLine("Не удалось задать цену запроса");
            }
            #endregion

            #region Категории
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
            #endregion

            #region Город
            try
            {
                requestBodyJson["variables"]["location"]["city"] = searchAttributes.locationId;
            }
            catch
            {
                Console.WriteLine("Не удалось задать город запроса");
            }
            #endregion

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
                bool isCorrect = false;

                if (JObject.Parse(item.ToString()).TryGetValue("product", out JToken productToken))
                {
                    JObject productJObject = JObject.Parse(productToken.ToString());

                    if (JObject.Parse(productJObject.ToString()).TryGetValue("id", out JToken idToken))
                    {
                        JObject idJObject = JObject.Parse(idToken.ToString());
                        product.Id = idJObject.ToString();
                        isCorrect = true;
                    }

                    if (JObject.Parse(productJObject.ToString()).TryGetValue("name", out JToken nameToken))
                    {
                        JObject nameJObject = JObject.Parse(nameToken.ToString());
                        product.Name = nameJObject.ToString();
                        isCorrect = true;
                    }
                }
                else
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
}
