﻿using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserYoula.Data;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using System.Web;



SearchBody searchBody = new SearchBody("https://youla.ru/all/zhivotnye?attributes[price][to]=200000&attributes[price][from]=0");

App parser = new App(searchBody);
await parser.Run();


public class App
{
    private readonly DataBaseContext context = new DataBaseContext();

    private readonly SearchBody searchBody;

    public List<Product> Parsed { get; set; } = new List<Product>();
    public List<Product> Valid { get; set; } = new List<Product>();
    public List<Product> Invalid { get; set; } = new List<Product>();


    public async Task Run()
    {
        while (true)
        {
            int page = 0;
            bool isEmpty = true;
            searchBody.Page = page;
            await foreach (var product in YoulaApi.GetProductsAsyncEnumerable(searchBody))
            {

                Console.WriteLine($"{product?.Id} {product?.Owner?.AnyCallEnabled}");

                isEmpty = false;
            }


            if (isEmpty) break;
            page++;
        }
    }



    public App(SearchBody search)
    {
        searchBody = search;
    }
}



public static class YoulaApi
{
    private static readonly HtmlParser parser = new HtmlParser();
    private static readonly HttpClient client = new HttpClient();


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
                ShortLinkYoula = x["id"]?.ToString() != null ? $"https://youla.ru/p{x["id"]?.ToString()}" : null,
                Name = x["name"]?.ToString(),
            })?
            .ToList() ?? new List<Product>();
        return products;
    }


    public static async Task<IEnumerable<Product>> GetProductsAsync(SearchBody searchBody)
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
                ShortLinkYoula = x["id"]?.ToString() != null ? $"https://youla.ru/p{x["id"]?.ToString()}" : null,
                Name = x["name"]?.ToString(),
            })?
            .ToList() ?? new List<Product>();


        products = products.Select(p => GetProductInfoAsync(p).Result).ToList();
        products = products.Select(p => GetOwnerInfoAsync(p).Result).ToList();

        return products;
    }

    public static async IAsyncEnumerable<Product> GetProductsAsyncEnumerable(SearchBody searchBody)
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
                ShortLinkYoula = x["id"]?.ToString() != null ? $"https://youla.ru/p{x["id"]?.ToString()}" : null,
                Name = x["name"]?.ToString(),
            })?
            .ToList() ?? new List<Product>();

        foreach (var product in products)
        {
            var productInfo = await GetProductInfoAsync(product);
            //var productWithOwnerInfo = await GetOwnerInfoAsync(productInfo);
            yield return productInfo;
        }
    }

    private static bool? BoolParse(string? value) => bool.TryParse(value, out var tempBool) ? tempBool : null;

    public static async Task<Product> GetProductInfoAsync(Product product)
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

    public static async Task<Product> GetOwnerInfoAsync(Product product)
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
    public SearchBody(string link)
    {
        Uri uri = new Uri(link);
        NameValueCollection queryParams = HttpUtility.ParseQueryString(uri.Query);
        Category = uri?.Segments?.LastOrDefault();
        PriceFrom = queryParams?.Get("attributes[price][from]");
        PriceTo = queryParams?.Get("attributes[price][to]");
        SortField = queryParams?.Get("attributes[sort_field]") ?? "DATE_PUBLISHED_DESC";
    }
}



public class Filter
{
    /// <summary>
    /// Отзывов больше чем
    /// </summary>
    public int RatingMarkCntMoreThen { get; set; } = 0;

    /// <summary>
    /// Отзывов меньше чем
    /// </summary>
    public int RatingMarkCntLessThen { get; set; } = 0;

    /// <summary>
    /// Может быть магазином
    /// </summary>
    public bool IncludeStores { get; set; } = false;

    /// <summary>
    /// Исключить слова из заголовка
    /// </summary>
    public List<string> ExcludeFromTitle { get; set; } = new List<string>();
    /// <summary>
    /// Исключить слова из текста
    /// </summary>
    public List<string> ExcludeFromDescription { get; set; } = new List<string>();

    public bool IsValid(Product? product)
    {
        if (product == null) return false;
        throw new NotImplementedException();
    }
}