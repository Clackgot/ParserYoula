using AngleSharp.Html.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserYoula.Data;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Web;


Console.WriteLine("Ссылка:");
string link = Console.ReadLine() ?? "";
SearchBody searchBody = new SearchBody(link);

App parser = new App(searchBody);
await parser.RunByTimezones();
Console.ReadKey();







public class App
{
    private readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
    private readonly CancellationToken token;

    private readonly DataBaseContext context = new DataBaseContext();
    private readonly ExcelContext excelContext = new ExcelContext();

    private readonly SearchBody searchBody;

    public List<Product> Parsed { get; set; } = new List<Product>();
    public List<Product> Valid { get; set; } = new List<Product>();
    public List<Product> Invalid { get; set; } = new List<Product>();

    private readonly Filter filter = new Filter();

    private bool CanCanceled = true;

    public async Task Run()
    {
        for (int page = 0; page < int.MaxValue; page++)
        {
            bool isEmpty = true;
            searchBody.Page = page;
            try
            {
                //await foreach (var product in YoulaApi.GetProductsAsyncEnumerable(searchBody, token))
                //{
                //    if (product == null) continue;
                //    if (filter.IsValid(product))
                //    {
                //        Console.ForegroundColor = ConsoleColor.Green;
                //        Valid.Add(product);
                //    }
                //    else
                //    {
                //        Console.ForegroundColor = ConsoleColor.Red;
                //        Invalid.Add(product);
                //    }
                //    Console.WriteLine($"{product?.ShortLinkYoula}");
                //    Console.ResetColor();
                //    isEmpty = false;
                //}
                foreach (var product in await YoulaApi.GetProductsAsync(searchBody, token))
                {
                    if (product == null) continue;
                    if (filter.IsValid(product))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Valid.Add(product);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Invalid.Add(product);
                    }
                    Console.WriteLine($"{product?.ShortLinkYoula}");
                    Console.ResetColor();
                    isEmpty = false;
                }
            }
            catch (OperationCanceledException ex)
            {
                if (CanCanceled) Save(this, null);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (isEmpty) break;
        }


        Save(this, null);
    }


    public async Task RunByTimezones()
    {

        List<string> timezones = JToken.Parse(File.ReadAllText("timezones.json")).ToList().Where(x => x != null).Select(x=>x.ToString()).ToList() ?? new List<string>();
        var citiesTokens = await YoulaApi.GetCitiesByTimezone(timezones);
        var rnd = new Random();
        var cities = citiesTokens.OrderBy(item => rnd.Next());
        foreach (var city in cities)
        {
            Console.WriteLine($"Начало поиска по городу {city["name"]} с часовым поясом {city["timezone"]}");
            searchBody.CityId = city["id"]?.ToString();
            for (int page = 0; page < int.MaxValue; page++)
            {
                bool isEmpty = true;
                searchBody.Page = page;
                try
                {
                    //await foreach (var product in YoulaApi.GetProductsAsyncEnumerable(searchBody, token))
                    //{
                    //    if (product == null) continue;
                    //    if (filter.IsValid(product))
                    //    {
                    //        Console.ForegroundColor = ConsoleColor.Green;
                    //        Valid.Add(product);
                    //    }
                    //    else
                    //    {
                    //        Console.ForegroundColor = ConsoleColor.Red;
                    //        Invalid.Add(product);
                    //    }
                    //    Console.WriteLine($"{product?.ShortLinkYoula}");
                    //    Console.ResetColor();
                    //    isEmpty = false;
                    //}
                    foreach (var product in await YoulaApi.GetProductsAsync(searchBody, token))
                    {
                        if (product == null) continue;
                        if (filter.IsValid(product))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Valid.Add(product);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Invalid.Add(product);
                        }
                        Console.WriteLine($"{product?.ShortLinkYoula}");
                        Console.ResetColor();
                        isEmpty = false;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (CanCanceled) Save(this, null);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                if (isEmpty) break;
            }
        }

        Save(this, null);
    }


    public App(SearchBody search)
    {
        searchBody = search;
        token = cancelTokenSource.Token;
        Console.CancelKeyPress += CancelToken;

        string filterPath = "filter.json";
        try
        {
            filter = JsonConvert.DeserializeObject<Filter>(File.ReadAllText(filterPath))!;
        }
        catch
        {
            filter = new Filter();
            try
            {
                File.WriteAllText(filterPath, JsonConvert.SerializeObject(filter));
            }
            catch
            {

            }
            //Console.CancelKeyPress += Save;
        }
    }

    private void CancelToken(object? sender, ConsoleCancelEventArgs e)
    {
        cancelTokenSource.Cancel();
        e.Cancel = true;
    }

    private void SaveToDb()
    {
        List<Product> withoutDublicates = Valid.Where(p => p?.Owner?.Id != null)
          .GroupBy(p => p.Owner!.Id)
          .Select(g => g.First())
          .ToList();
        Console.WriteLine($"Удалено дублей: [{Valid.Count - withoutDublicates.Count}]");
        Valid = withoutDublicates;

        //List<User> validUsers = Valid?.Where(p => p?.Owner != null).Select(p => p.Owner!).ToList() ?? new List<User>();
        ////validUsers = validUsers.Except(context.Users).ToList();
        //List<Product> uniqProducts = Valid!.Except(validUsers.Select(
        //    x=>new Product() { Owner = new User() { Id = x.Id} }
        //    )).ToList();

        List<Product>? uniqProducts = new List<Product>();

        foreach (var product in Valid)
        {
            if (!context.Users.Contains(product?.Owner))
            {
                if (product != null) uniqProducts.Add(product);
            }
        }

        Console.WriteLine($"Уже в базе: [{Valid!.Count - uniqProducts.Count}]");
        Valid = uniqProducts;

        context.AddRange(Valid.Where(p => p?.Owner != null).Select(p => p.Owner!));
        int addedCount = context.SaveChanges();
        Console.WriteLine($"Записано в базу: [{addedCount}]");
    }

    private void Save(object? sender, ConsoleCancelEventArgs e)
    {
        CanCanceled = false;
        Console.WriteLine($"Валид: [{Valid.Count}]");
        Console.WriteLine($"Невалид: [{Invalid.Count}]");
        SaveToDb();
        excelContext.Save(Valid);
    }
}



public static class YoulaApi
{
    private static readonly HtmlParser parser = new HtmlParser();
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string?> GetTimeZoneByCityId(string? cityId)
    {
        JObject citiesWithTimezone = await GetCitiesWithTimezone();
        return citiesWithTimezone["data"]?.FirstOrDefault(x => x["id"]?.ToString() == cityId)?["timezone"]?.ToString();
    }

    public static async Task<List<JToken>> GetCitiesByTimezone(List<string> timezones)
    {
        JObject citiesWithTimezone = await GetCitiesWithTimezone();
        List<JToken> cities = new List<JToken>();
        //return citiesWithTimezone["data"]?.FirstOrDefault(x => x["id"]?.ToString() == cityId)?["timezone"]?.ToString();
        foreach (var timezone in timezones)
        {
            foreach (var city in citiesWithTimezone["data"]?.Where(city => city["timezone"]?.ToString() == timezone))
            {
                cities.Add(city);
            }
        }
        return cities;
    }


    public static async Task<JObject> GetCitiesWithTimezone()
    {
        var jsonText = await File.ReadAllTextAsync("cities.json");
        var citiesWithTimezone = JObject.Parse(jsonText);
        return citiesWithTimezone;
    }

    public static async Task<string?> GetCityIdBySlugAsync(string? cytySlug)
    {
        if (cytySlug == null) return null;
        try
        {
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://api.youla.io/api/v1/geo/cities"),
            };
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9");
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "cross-site");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.62 Safari/537.36");
            request.Headers.Add("accept", "*/*");
            request.Headers.Add("appId", "web/3");
            request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"102\", \"Google Chrome\";v=\"102\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("x-app-id", "web/3");
            request.Headers.Add("x-youla-splits", "8a=5|8b=7|8c=0|8m=0|8v=0|8z=0|16a=0|16b=0|64a=6|64b=0|100a=73|100b=47|100c=0|100d=0|100m=0");


            HttpResponseMessage? response = await client.SendAsync(request);
            string? jsonString = await response.Content.ReadAsStringAsync();

            JToken? json = JToken.Parse(jsonString)["data"];
            return json?.FirstOrDefault(x => x["slug"]?.ToString() == cytySlug)?["id"]?.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine($"Не удалось получить информацию о городе {cytySlug}. Будет выбрана вся Россия.");
            return null;
        }
    }


    public static async Task<IEnumerable<Product>> GetClearProductsAsync(SearchBody searchBody)
    {
        searchBody = searchBody ?? throw new ArgumentNullException(nameof(searchBody));
        HttpRequestMessage request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://api-gw.youla.io/federation/graphql"),
        };

        request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9");
        request.Headers.Add("Connection", "keep-alive");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.62 Safari/537.36");
        request.Headers.Add("accept", "*/*");
        request.Headers.Add("appId", "web/3");
        request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"102\", \"Google Chrome\";v=\"102\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("x-app-id", "web/3");
        request.Headers.Add("x-youla-splits", "8a=5|8b=7|8c=0|8m=0|8v=0|8z=0|16a=0|16b=0|64a=6|64b=0|100a=73|100b=47|100c=0|100d=0|100m=0");

        var body = $"{{\"operationName\":\"catalogProductsBoard\",\"variables\":{{\"sort\":\"{searchBody.SortField}\",\"attributes\":[{{\"slug\":\"price\",\"value\":null,\"from\":{searchBody.PriceFrom},\"to\":{searchBody.PriceTo}}},{{\"slug\":\"categories\",\"value\":[\"{searchBody.Category}\"],\"from\":null,\"to\":null}}],\"datePublished\":null,\"location\":{{\"latitude\":null,\"longitude\":null,\"city\":null,\"distanceMax\":null}},\"search\":\"\",\"cursor\":\"{{\\\"page\\\":{searchBody.Page - 1},\\\"totalProductsCount\\\":0,\\\"dateUpdatedTo\\\":1653932782}}\"}},\"extensions\":{{\"persistedQuery\":{{\"version\":1,\"sha256Hash\":\"6e7275a709ca5eb1df17abfb9d5d68212ad910dd711d55446ed6fa59557e2602\"}}}}}}";
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage? response = await client.SendAsync(request);
        string? jsonString = await response.Content.ReadAsStringAsync();

        List<Product>? products = JToken.Parse(jsonString)?["data"]?["feed"]?["items"]?
            .Where(i => !string.IsNullOrEmpty(i["product"]?["id"]?.ToString()))?
            .Select(x => x["product"]!)?
            .Select(x => new Product()
            {
                Id = x["id"]?.ToString(),
                Name = x["name"]?.ToString(),
            })?
            .ToList() ?? new List<Product>();
        return products;
    }

    public static async Task<IEnumerable<Product>> GetProductsAsync(SearchBody searchBody, CancellationToken token = default)
    {
        searchBody = searchBody ?? throw new ArgumentNullException(nameof(searchBody));
        HttpRequestMessage request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://api-gw.youla.io/federation/graphql"),
        };

        request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9");
        request.Headers.Add("Connection", "keep-alive");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.62 Safari/537.36");
        request.Headers.Add("accept", "*/*");
        request.Headers.Add("appId", "web/3");
        request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"102\", \"Google Chrome\";v=\"102\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("x-app-id", "web/3");
        request.Headers.Add("x-youla-splits", "8a=5|8b=7|8c=0|8m=0|8v=0|8z=0|16a=0|16b=0|64a=6|64b=0|100a=73|100b=47|100c=0|100d=0|100m=0");

        var body = $"{{\"operationName\":\"catalogProductsBoard\",\"variables\":{{\"sort\":\"{searchBody.SortField}\",\"attributes\":[{{\"slug\":\"price\",\"value\":null,\"from\":{searchBody.PriceFrom ?? "null"},\"to\":{searchBody.PriceTo ?? "null"}}},{{\"slug\":\"categories\",\"value\":[\"{searchBody.Category}\"],\"from\":null,\"to\":null}}],\"datePublished\":{{\"to\":{DateTimeOffset.Now.ToUnixTimeSeconds()},\"from\":{DateTimeOffset.Now.ToUnixTimeSeconds() - 86400}}},\"location\":{{\"latitude\":null,\"longitude\":null,\"city\":{(searchBody.CityId != null ? $"\"{searchBody.CityId}\"" : "null")},\"distanceMax\":null}},\"search\":\"\",\"cursor\":\"{{\\\"page\\\":{searchBody.Page - 1},\\\"totalProductsCount\\\":0,\\\"dateUpdatedTo\\\":1653932782}}\"}},\"extensions\":{{\"persistedQuery\":{{\"version\":1,\"sha256Hash\":\"6e7275a709ca5eb1df17abfb9d5d68212ad910dd711d55446ed6fa59557e2602\"}}}}}}";
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage? response = await client.SendAsync(request);
        string? jsonString = await response.Content.ReadAsStringAsync();

        List<Product>? products = JToken.Parse(jsonString)?["data"]?["feed"]?["items"]?
            .Where(i => !string.IsNullOrEmpty(i["product"]?["id"]?.ToString()))?
            .Select(x => x["product"]!)?
            .Select(x => new Product()
            {
                Id = x["id"]?.ToString(),
                Name = x["name"]?.ToString(),
                IsPromoted = BoolParse(x["isPromoted"]?.ToString()),
            })?
            .ToList() ?? new List<Product>();
        products = products.Where(p => p.IsPromoted != true).ToList();
        token.ThrowIfCancellationRequested();
        //products = products.Select(p => GetProductInfoAsync(p).Result).ToList();
        //token.ThrowIfCancellationRequested();
        //products = products.Select(p => GetOwnerInfoAsync(p).Result).ToList();
        List<Product> tempValids = new List<Product>();
        List<Product> tempValids2 = new List<Product>();
        Parallel.ForEach(products, p => tempValids.Add(GetProductInfoAsync(p).Result));
        Parallel.ForEach(tempValids, p => tempValids2.Add(GetOwnerInfoAsync(p).Result));

        return tempValids;
    }

    public static async IAsyncEnumerable<Product> GetProductsAsyncEnumerable(SearchBody searchBody, [EnumeratorCancellation] CancellationToken token = default)
    {
        searchBody = searchBody ?? throw new ArgumentNullException(nameof(searchBody));
        HttpRequestMessage request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://api-gw.youla.io/federation/graphql"),
        };

        request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9");
        request.Headers.Add("Connection", "keep-alive");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.62 Safari/537.36");
        request.Headers.Add("accept", "*/*");
        request.Headers.Add("appId", "web/3");
        request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"102\", \"Google Chrome\";v=\"102\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("x-app-id", "web/3");
        request.Headers.Add("x-youla-splits", "8a=5|8b=7|8c=0|8m=0|8v=0|8z=0|16a=0|16b=0|64a=6|64b=0|100a=73|100b=47|100c=0|100d=0|100m=0");

        var body = $"{{\"operationName\":\"catalogProductsBoard\",\"variables\":{{\"sort\":\"{searchBody.SortField}\",\"attributes\":[{{\"slug\":\"price\",\"value\":null,\"from\":{searchBody.PriceFrom ?? "null"},\"to\":{searchBody.PriceTo ?? "null"}}},{{\"slug\":\"categories\",\"value\":[\"{searchBody.Category}\"],\"from\":null,\"to\":null}}],\"datePublished\":null,\"location\":{{\"latitude\":null,\"longitude\":null,\"city\":null,\"distanceMax\":null}},\"search\":\"\",\"cursor\":\"{{\\\"page\\\":{searchBody.Page - 1},\\\"totalProductsCount\\\":0,\\\"dateUpdatedTo\\\":1653932782}}\"}},\"extensions\":{{\"persistedQuery\":{{\"version\":1,\"sha256Hash\":\"6e7275a709ca5eb1df17abfb9d5d68212ad910dd711d55446ed6fa59557e2602\"}}}}}}";
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage? response = await client.SendAsync(request);
        string? jsonString = await response.Content.ReadAsStringAsync();

        List<Product>? products = JToken.Parse(jsonString)?["data"]?["feed"]?["items"]?
            .Where(i => !string.IsNullOrEmpty(i["product"]?["id"]?.ToString()))?
            .Select(x => x["product"]!)?
            .Select(x => new Product()
            {
                Id = x["id"]?.ToString(),
                Name = x["name"]?.ToString(),
                IsPromoted = BoolParse(x["isPromoted"]?.ToString()),
            })?
            .ToList() ?? new List<Product>();

        int count = products.Count;
        products = products.Where(p => p.IsPromoted != true).ToList();
        Console.WriteLine($"Удалено: {count - products.Count}");

        token.ThrowIfCancellationRequested();
        foreach (var product in products)
        {

            var productInfo = await GetProductInfoAsync(product);
            //var productWithOwnerInfo = await GetOwnerVkInfoAsync(productInfo);
            var productWithOwnerInfo = await GetOwnerInfoAsync(productInfo);

            token.ThrowIfCancellationRequested();

            yield return productInfo;
        }
    }

    private static bool? BoolParse(string? value) => bool.TryParse(value, out var tempBool) ? tempBool : null;

    private static int? IntParse(string? value) => int.TryParse(value, out var tempBool) ? tempBool : null;


    public static async Task<Product> GetProductInfoAsync(Product product, CancellationToken token = default)
    {
        product = product ?? throw new ArgumentNullException(nameof(product));

        HttpRequestMessage request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"https://api.youla.io/api/v1/product/{product.Id}"),
        };

        HttpResponseMessage? response = await client.SendAsync(request);
        string? jsonString = await response.Content.ReadAsStringAsync();

        JToken? productToken = JObject.Parse(jsonString)?["data"];

        product.IsPublished = BoolParse(productToken?["is_published"]?.ToString());
        product.IsSold = BoolParse(productToken?["is_sold"]?.ToString());
        product.IsDeleted = BoolParse(productToken?["is_deleted"]?.ToString());
        product.IsBlocked = BoolParse(productToken?["is_blocked"]?.ToString());
        product.IsArchived = BoolParse(productToken?["is_archived"]?.ToString());
        product.IsExpired = BoolParse(productToken?["is_expiring"]?.ToString());
        product.Description = productToken?["description"]?.ToString();
        product.CreateDate = productToken?["date_created"]?.ToString();
        product.UpdateDate = productToken?["date_updated"]?.ToString();
        product.PublishDate = productToken?["date_published"]?.ToString();


        product.Owner = productToken?["owner"] == null ? null : new User()
        {
            Id = productToken?["owner"]?["id"]?.ToString(),
            Name = productToken?["owner"]?["name"]?.ToString(),
            Phone = productToken?["owner"]?["display_phone_num"]?.ToString(),
            IsShop = productToken?["owner"]?["store"] != null,
            AnyCallEnabled = BoolParse(productToken?["owner"]?["settings"]?["call_settings"]?["any_call_enabled"]?.ToString()),
            P2pCallEnabled = BoolParse(productToken?["owner"]?["settings"]?["call_settings"]?["p2p_call_enabled"]?.ToString()),
            SystemCallEnabled = BoolParse(productToken?["owner"]?["settings"]?["call_settings"]?["system_call_enabled"]?.ToString()),
        };

        return product;

    }

    public static async Task<Product> GetOwnerInfoAsync(Product product, CancellationToken token = default)
    {
        product = product ?? throw new ArgumentNullException(nameof(product));
        if (product.Owner?.Id == null) return product;
        HttpRequestMessage request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"https://api.youla.io/api/v1/user/{product?.Owner?.Id}"),
        };

        HttpResponseMessage? response = await client.SendAsync(request);
        string? jsonString = await response.Content.ReadAsStringAsync();

        JToken? ownerToken = JObject.Parse(jsonString)?["data"];

        product!.Owner.RatingMarkCount = IntParse(ownerToken?["rating_mark_cnt"]?.ToString());
        return product;

    }


    /// <summary>
    /// Получить информацию о продавце в ВК
    /// </summary>
    /// <param name="product"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<Product> GetOwnerVkInfoAsync(Product product)
    {
        product = product ?? throw new ArgumentNullException(nameof(product));
        if (product.Owner == null) return product;

        HttpRequestMessage request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"https://api.vk.com/method/classifieds.getUserContactInfo?v=5.157&product_id={product.Id}&access_token=4cfe1a7e2ab34f833bacdc86c1021df33562486585bae2ce356ae6473aed705abc3b2e14c5f8ca9685d57"),
        };

        HttpResponseMessage? response = await client.SendAsync(request);
        string? jsonString = await response.Content.ReadAsStringAsync();

        JToken? responseToken = JObject.Parse(jsonString)?["response"];
        product.Owner.IsChatLocked = BoolParse(responseToken?["is_chat_locked"]?.ToString());
        product.Owner.IsPhoneLocked = BoolParse(responseToken?["is_phone_locked"]?.ToString());
        product.Owner.IsPhoneDisabled = BoolParse(responseToken?["is_phone_disabled"]?.ToString());
        product.Owner.DisableCallAlertText = responseToken?["disable_call_alert"]?["text"]?.ToString();
        return product;
    }
}


public class SearchBody
{
    public int Page { get; set; }
    public string? PriceFrom { get; set; }
    public string? PriceTo { get; set; }
    public string? Category { get; set; }
    public string? SortField { get; set; }
    private string? CitySlug { get; set; }
    public string? CityId { get; set; }
    public SearchBody(string link)
    {
        Uri uri = new Uri(link);
        NameValueCollection queryParams = HttpUtility.ParseQueryString(uri.Query);
        CitySlug = new string(uri?.Segments?.Skip(1).FirstOrDefault()?.SkipLast(1)?.ToArray() ?? null);
        CityId = YoulaApi.GetCityIdBySlugAsync(CitySlug).Result ?? null;
        Category = uri?.Segments?.LastOrDefault();
        PriceFrom = queryParams?.Get("attributes[price][from]");
        PriceTo = queryParams?.Get("attributes[price][to]");
        SortField = "DATE_PUBLISHED_DESC";
    }
}




public class Filter
{
    private List<Predicate<Product>> FilterFuncionns = new List<Predicate<Product>>();


    /// <summary>
    /// Отзывов больше чем
    /// </summary>
    public int RatingMarkCntMoreThen { get; set; } = -1;

    /// <summary>
    /// Отзывов меньше чем
    /// </summary>
    public int RatingMarkCntLessThen { get; set; } = 3;

    /// <summary>
    /// Может быть магазином
    /// </summary>
    public bool IncludeStores { get; set; } = false;

    /// <summary>
    /// Исключить слова из заголовка
    /// </summary>
    public List<string> ExcludeWordsFromTitle { get; set; } = new List<string>() { "кошка", "сено" };
    /// <summary>
    /// Исключить слова из текста
    /// </summary>
    public List<string> ExcludeWordsFromDescription { get; set; } = new List<string>() { "кошка", "сено" };
    private bool RatingMarksValid(Product product)
    {
        int? marksCount = product?.Owner?.RatingMarkCount;
        bool ratingValid = marksCount != null ?
            marksCount < RatingMarkCntLessThen && marksCount > RatingMarkCntMoreThen :
            false;
        return ratingValid;
    }
    private bool NotContainWordsFormBlackListInTitle(Product product)
    {
        if (string.IsNullOrEmpty(product.Name)) return true;

        string title = product.Name;

        foreach (string? blackListWord in ExcludeWordsFromTitle)
        {
            int i = title.ToLowerInvariant().IndexOf(blackListWord.ToLowerInvariant());
            if (i >= 0)
            {
                var start = title[..i];
                var middle = title.Substring(i, blackListWord.Length);
                var end = title[(i + blackListWord.Length)..];
                Console.Write(start);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(middle);
                Console.ResetColor();
                Console.WriteLine(end);
                return false;
            }
        }
        return true;
    }
    private bool NotContainWordsFormBlackListInDescription(Product product)
    {
        if (string.IsNullOrEmpty(product.Description)) return true;

        string description = product.Description.Trim().Replace("\n", " ");

        foreach (string? blackListWord in ExcludeWordsFromDescription)
        {
            int i = description.ToLowerInvariant().IndexOf(blackListWord.ToLowerInvariant());
            if (i >= 0)
            {
                var start = description[..i];
                var middle = description.Substring(i, blackListWord.Length);
                var end = description[(i + blackListWord.Length)..];
                Console.Write(start);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(middle);
                Console.ResetColor();
                Console.WriteLine(end);
                return false;
            }
        }
        return true;
    }

    private bool ProductStateAvaible(Product product)
    {
        bool isSold = product.IsSold ?? false;
        bool isArchived = product.IsArchived ?? false;
        bool isDeleted = product.IsDeleted ?? false;
        bool isBlocked = product.IsBlocked ?? false;
        //bool isExpired = product.IsExpired ?? false;

        bool valid = !isSold && !isArchived && !isDeleted && !isBlocked /*&& !isExpired*/;
        if (!valid)
        {
            if (isSold) { Console.ForegroundColor = ConsoleColor.Red; Console.Write("[продано]"); Console.ResetColor(); }
            else { Console.ForegroundColor = ConsoleColor.Green; Console.Write("[не продано]"); Console.ResetColor(); }

            if (isArchived) { Console.ForegroundColor = ConsoleColor.Red; Console.Write("[архив]"); Console.ResetColor(); }
            else { Console.ForegroundColor = ConsoleColor.Green; Console.Write("[не архив]"); Console.ResetColor(); }

            if (isDeleted) { Console.ForegroundColor = ConsoleColor.Red; Console.Write("[удалено]"); Console.ResetColor(); }
            else { Console.ForegroundColor = ConsoleColor.Green; Console.Write("[не удалено]"); Console.ResetColor(); }

            if (isBlocked) { Console.ForegroundColor = ConsoleColor.Red; Console.Write("[заблокировано]"); Console.ResetColor(); }
            else { Console.ForegroundColor = ConsoleColor.Green; Console.Write("[не заблокировано]"); Console.ResetColor(); }

            //if (isExpired) { Console.ForegroundColor = ConsoleColor.Red; Console.Write("[просрочено]"); Console.ResetColor(); }
            //else { Console.ForegroundColor = ConsoleColor.Green; Console.Write("[не просрочено]"); Console.ResetColor(); }

            Console.WriteLine();
        }
        return valid;
    }

    public bool IsValid(Product? product)
    {
        if (product == null) return false;
        if (product?.Owner?.IsShop == true) return false;
        FilterFuncionns.Add(NotContainWordsFormBlackListInTitle);
        FilterFuncionns.Add(NotContainWordsFormBlackListInDescription);
        FilterFuncionns.Add(ProductStateAvaible);
        FilterFuncionns.Add(RatingMarksValid);


        foreach (var check in FilterFuncionns)
        {
            if (!check(product))
            {
                return false;
            }
        }

        return true;
    }

}