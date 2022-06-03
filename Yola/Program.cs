using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using System.Web;

SearchBody searchBody = new SearchBody("https://youla.ru/all/zhivotnye?attributes[price][to]=200000&attributes[price][from]=0");





//while (true)
//{
//    int page = 0;
//    bool isEmpty = true;
//    searchBody.Page = page;
//    await foreach (var product in Graphql.GetProductsAsyncEnumerable(searchBody))
//    {
//        Console.WriteLine($"{product?.Id} {product?.Owner?.IsChatLocked} {product?.Owner?.IsPhoneLocked} {product?.Owner?.IsPhoneDisabled}");
//        isEmpty = false;
//    }
//    if (isEmpty) break;
//    page++;
//}




public static class Graphql
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
            .Where(i=>!string.IsNullOrEmpty(i["product"]?["id"]?.ToString()))?
            .Select(x=>x["product"]!)?
            .Select(x=> new Product() 
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
            IsShop = BoolParse(productToken?["owner"]?["is_shop"]?.ToString()),
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


public class Product : IEquatable<Product?>
{

    public string? Id { get; set; }
    public string? ShortLinkYoula { get; set; }
    public string? ShortLinkVk { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PublishDate { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsSold { get; set; }
    public bool? IsExpired { get; set; }
    public bool? IsBlocked { get; set; }
    public bool? IsArchived { get; set; }
    public bool? IsDeleted { get; internal set; }
    public User? Owner { get; set; }
    public override bool Equals(object? other)
    {
        //Последовательность проверки должна быть именно такой.
        //Если не проверить на null объект other, то other.GetType() может выбросить //NullReferenceException.            
        if (other == null)
            return false;

        //Если ссылки указывают на один и тот же адрес, то их идентичность гарантирована.
        if (object.ReferenceEquals(this, other))
            return true;

        //Если класс находится на вершине иерархии или просто не имеет наследников, то можно просто
        //сделать Vehicle tmp = other as Vehicle; if(tmp==null) return false; 
        //Затем вызвать экземплярный метод, сразу передав ему объект tmp.
        if (this.GetType() != other.GetType())
            return false;

        return this.Equals(other as Product);

    }
    public bool Equals(Product? other)
    {
        if (other == null)
            return false;

        //Здесь сравнение по ссылкам необязательно.
        //Если вы уверены, что многие проверки на идентичность будут отсекаться на проверке по ссылке - //можно имплементировать.
        if (object.ReferenceEquals(this, other))
            return true;


        return other.Id == this.Id || other?.Owner?.Id == this?.Owner?.Id;
    }

    public static bool operator ==(Product? left, Product? right) => left?.Id == right?.Id || left?.Owner?.Id == right?.Owner?.Id;
    public static bool operator !=(Product? left, Product? right) => !(left == right);

    public override int GetHashCode() => HashCode.Combine(Id);
    
}

public class User : IEquatable<User?>
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? CallTime { get; set; }
    public bool? IsShop { get; set; }



    public bool? IsChatLocked { get; set; }
    public bool? IsPhoneLocked { get; set; }
    public bool? IsPhoneDisabled { get; set; }
    public string? DisableCallAlertText { get; set; }

    public override bool Equals(object? other)
    {
        //Последовательность проверки должна быть именно такой.
        //Если не проверить на null объект other, то other.GetType() может выбросить //NullReferenceException.            
        if (other == null)
            return false;

        //Если ссылки указывают на один и тот же адрес, то их идентичность гарантирована.
        if (object.ReferenceEquals(this, other))
            return true;

        //Если класс находится на вершине иерархии или просто не имеет наследников, то можно просто
        //сделать Vehicle tmp = other as Vehicle; if(tmp==null) return false; 
        //Затем вызвать экземплярный метод, сразу передав ему объект tmp.
        if (this.GetType() != other.GetType())
            return false;

        return this.Equals(other as User);

    }
    public bool Equals(User? other)
    {
        if (other == null)
            return false;

        //Здесь сравнение по ссылкам необязательно.
        //Если вы уверены, что многие проверки на идентичность будут отсекаться на проверке по ссылке - //можно имплементировать.
        if (object.ReferenceEquals(this, other))
            return true;

        return other.Id == this.Id;
    }
    public static bool operator ==(User? left, User? right) => left?.Id == right?.Id;
    public static bool operator !=(User? left, User? right) => !(left == right);
    public override int GetHashCode() => HashCode.Combine(Id);


}
