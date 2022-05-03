using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

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
        private YoulaDataBase db = new YoulaDataBase("data.db");


        public Parser()
        {

        }

        public async Task Run()
        {
            Console.WriteLine("Ссылка:");
            string link = Console.ReadLine();

            //string link = @"https://youla.ru/novoorsk/zhivotnye/gryzuny?attributes[price][from]=50000";
            //string link = @"https://youla.ru/novoorsk/zhivotnye?attributes[price][to]=100000000&attributes[price][from]=100";
            //string link = @"https://youla.ru/novoorsk/zhivotnye/koshki";


            SearchAttributes result = await ParseParamsFromLink(link);
            List<Product> products = new List<Product>();
            await foreach (var product in GetAllProducts(result))
            {
                products.Add(product);
                Console.WriteLine($"Найдено {products.Count} объявлений");
            }
            //if (!string.IsNullOrEmpty(result.subcategorySlug))
            //{
            //    Save(products, result.subcategorySlug);
            //}
            //else
            //{
            //    Save(products, result.categorySlug);
            //}

            YoulaDataBase dataBase = new YoulaDataBase("profiles.db");
            dataBase.Create();
            List<Product> addedProducts = dataBase.AddProducts(products);

            if (!string.IsNullOrEmpty(result.subcategorySlug))
            {
                Save(addedProducts, result.subcategorySlug);
            }
            else
            {
                Save(addedProducts, result.categorySlug);
            }

        }


        private static void Save(List<Product> products, string listName)
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
                if (int.TryParse(jsonResponse["data"]["owner"]["rating_mark_cnt"].ToString(), out marksCount))
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












    class YoulaDataBase
    {
        private string dbFileName;

        public string DbFileName
        {
            get { return dbFileName; }
        }
        private SQLiteConnection connection;

        public SQLiteConnection Connection
        {
            get { return connection; }
        }

        private SQLiteCommand command;

        public SQLiteCommand Command
        {
            get { return command; }
        }

        public YoulaDataBase(string name)
        {
            connection = new SQLiteConnection();
            command = new SQLiteCommand();
            dbFileName = name;
        }

        public void Create()
        {
            bool newDB = false;
            if (!File.Exists(dbFileName))
            {
                SQLiteConnection.CreateFile(dbFileName);
                newDB = true;
            }

            try
            {
                connection = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
                connection.Open();
                command.Connection = connection;

                if (newDB)
                {
                    command.CommandText = @"CREATE TABLE products (id	INTEGER NOT NULL UNIQUE, productId	TEXT NOT NULL UNIQUE,ownerId	TEXT NOT NULL UNIQUE,description	TEXT,price	INTEGER,marks INTEGER check(marks >= 0 and marks <= 2),PRIMARY KEY(id AUTOINCREMENT))";
                    command.ExecuteNonQuery();
                    AddTestData();
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("Disconnected");
                Console.WriteLine(ex.Message);
            }
        }

        private void AddProductsData()
        {
            command.CommandText = "INSERT INTO products (productId, ownerId, description, price, marks) VALUES (:productId, :ownerId, :description, :price, :marks)";
            SQLiteTransaction transaction = connection.BeginTransaction();//запускаем транзакцию
            try
            {
                for (int i = 1; i < 101; i++)
                {
                    command.Parameters.AddWithValue("productId", $"product_{i}");
                    command.Parameters.AddWithValue("ownerId", $"owner_{i}");
                    command.Parameters.AddWithValue("description", $"description_{i}");
                    command.Parameters.AddWithValue("price", i+2);
                    command.Parameters.AddWithValue("marks", i*2);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine(e.Message);
                throw;
            }
        }


        private void AddTestData()
        {
            //AddProductsData();
        }

        public List<Product> AddProducts(List<Product> products)
        {
            List<Product> addedProducts = new List<Product>();
            command.CommandText = "INSERT or IGNORE INTO products (productId, ownerId, description, price, marks) VALUES (:productId, :ownerId, :description, :price, :marks)";
            SQLiteTransaction transaction = connection.BeginTransaction();//запускаем транзакцию
            try
            {
                foreach (var product in products)
                {
                    command.Parameters.AddWithValue("productId", product.Id);
                    command.Parameters.AddWithValue("ownerId", product.OwnerId);
                    command.Parameters.AddWithValue("description", product.Description);
                    command.Parameters.AddWithValue("price", product.Price);
                    command.Parameters.AddWithValue("marks", product.MarksCount);
                    
                    bool isAdded = Convert.ToBoolean(command.ExecuteNonQuery());
                    if (isAdded) addedProducts.Add(product);
                }
                transaction.Commit();
            }
            catch (Exception e)
            {
                transaction.Rollback();
                Console.WriteLine(e.Message);
                throw;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Найдено новых объявлений: {addedProducts.Count}");
            Console.ForegroundColor = ConsoleColor.Gray;
            return addedProducts;
        }


        public void PrintProducts()
        {
            command.CommandText = "SELECT * FROM products";
            DataTable data = new DataTable();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
            adapter.Fill(data);
            Console.WriteLine($"Прочитано {data.Rows.Count} записей из таблицы products");
            foreach (DataRow row in data.Rows)
            {
                Console.WriteLine($"id = {row.Field<long>("id")} " +
                    $"productId = {row.Field<string>("productId")} " +
                    $"ownerId = {row.Field<string>("ownerId")} " +
                    $"description = {row.Field<string>("description")} " +
                    $"price = {row.Field<long>("price")} " +
                    $"marks = {row.Field<long>("marks")} ");
            }
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser parser = new Parser();
            parser.Run().Wait();

            //if (Connect("firstBase.db"))
            //{
            //    Console.WriteLine("Connected");
            //}





        }
    }
}
