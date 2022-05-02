using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Console.WriteLine("Link:");
            string link = Console.ReadLine();

            //string link = @"https://youla.ru/novoorsk/zhivotnye/gryzuny?attributes[price][from]=50000";
            //string link = @"https://youla.ru/novoorsk/zhivotnye?attributes[price][to]=100000000&attributes[price][from]=100";
            //string link = @"https://youla.ru/novoorsk/zhivotnye/koshki";


            SearchAttributes result = await ParseParamsFromLink(link);
            List<Product> products = new List<Product>();
            await foreach (var product in GetAllProducts(result))
            {
                products.Add(product);
            }
            Save(products);
        }


        private static void Save(List<Product> products)
        {
            var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("Result");
            sheet.Cells[1, 1].Value = "ID объявление";
            sheet.Cells[1, 2].Value = "ID владельца";
            sheet.Cells[1, 3].Value = "Описание";
            sheet.Cells[1, 4].Value = "Отзывов";
            sheet.Cells[1, 5].Value = "Название";
            sheet.Cells[1, 6].Value = "Цена";
            int row = 2;
            int col = 1;

            //List<Product> products = new List<Product>();
            //for (int i = 0; i < 10; i++)
            //{
            //    products.Add(new Product()
            //    {
            //        Id = $"Id{i}",
            //        OwnerId = $"OwnerId{i}",
            //        Description = $"Description{i}",
            //        MarksCount = i,
            //        Name = $"Name{i}",
            //        Price = i * 100
            //    });
            //}


            foreach (var product in products)
            {
                sheet.Cells[row, col].Value = product.Id;
                sheet.Cells[row, col + 1].Value = product.OwnerId;
                sheet.Cells[row, col + 2].Value = product.Description;
                sheet.Cells[row, col + 3].Value = product.MarksCount;
                sheet.Cells[row, col + 4].Value = product.Name;
                sheet.Cells[row, col + 5].Value = product.Price;
                row++;
            }

            sheet.Protection.IsProtected = true;
            var excel = package.GetAsByteArray();
            File.WriteAllBytes("result.xlsx", excel);
        }


        public async IAsyncEnumerable<Product> GetAllProducts(SearchAttributes searchAttributes)
        {
            Console.WriteLine("ID\t OwnerID\t Name\t Price\t Marks\t");
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
                if(int.TryParse(jsonResponse["data"]["owner"]["rating_mark_cnt"].ToString(), out marksCount))
                {
                    product.MarksCount = marksCount;
                }
                else
                {
                    product.MarksCount = null;
                }
                product.Description = jsonResponse["data"]["description"].ToString();
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




            //Save();

            //string fileName = @"result.xlsx";
            //using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //{
            //    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(fs, false))
            //    {
            //        WorkbookPart workbookPart = doc.WorkbookPart;
            //        SharedStringTablePart sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().First();
            //        SharedStringTable sst = sstpart.SharedStringTable;
            //        WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
            //        Worksheet sheet = worksheetPart.Worksheet;
            //        var cells = sheet.Descendants<Cell>();
            //        var rows = sheet.Descendants<Row>();
            //        Console.WriteLine("Row count = {0}", rows.LongCount());
            //        Console.WriteLine("Cell count = {0}", cells.LongCount());
            //        // One way: go through each cell in the sheet
            //        foreach (Cell cell in cells)
            //        {
            //            if ((cell.DataType != null) && (cell.DataType == CellValues.SharedString))
            //            {
            //                int ssid = int.Parse(cell.CellValue.Text);
            //                string str = sst.ChildElements[ssid].InnerText;
            //                Console.WriteLine("Shared string {0}: {1}", ssid, str);
            //            }
            //            else if (cell.CellValue != null)
            //            {
            //                Console.WriteLine("Cell contents: {0}", cell.CellValue.Text);
            //            }
            //        }
            //        // Or... via each row
            //        foreach (Row row in rows)
            //        {
            //            foreach (Cell c in row.Elements<Cell>())
            //            {
            //                if ((c.DataType != null) && (c.DataType == CellValues.SharedString))
            //                {
            //                    int ssid = int.Parse(c.CellValue.Text);
            //                    string str = sst.ChildElements[ssid].InnerText;
            //                    Console.WriteLine("Shared string {0}: {1}", ssid, str);
            //                }
            //                else if (c.CellValue != null)
            //                {
            //                    Console.WriteLine("Cell contents: {0}", c.CellValue.Text);
            //                }
            //            }
            //        }
            //    }
            //}
        }


    }
}
