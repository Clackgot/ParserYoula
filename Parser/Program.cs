using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Parser
{

    public class DateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(long?));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //if (objectType == typeof(long?))
            //{
            //    long? stamp = serializer.Deserialize<long?>(reader);
            //    if (stamp == null)
            //    {
            //        return null;
            //    }
            //    else
            //    {
            //        return Tools.ConvertFromUnixTimestamp((double)stamp);
            //    }
            //}
            //else
            //{
            //    return null;
            //}
            

            return serializer.Deserialize<long?>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public static class Tools
    {
        public static DateTime ConvertFromUnixTimestamp(long timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }
        public static long ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return Convert.ToInt64(diff.TotalSeconds);
        }
    }

    public class Data
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        #region Даты
        [JsonProperty("date_created")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime? DateCreated { get; set; }
        [JsonProperty("date_updated")]
        public long? DateUpdated { get; set; }
        [JsonProperty("date_published")]
        public long? DatePublished { get; set; }
        [JsonProperty("date_sold")]
        public long? DateSold { get; set; }
        [JsonProperty("date_blocked")]
        public long? DateBlocked { get; set; }
        [JsonProperty("date_deleted")]
        public long? DateDeleted { get; set; }
        [JsonProperty("date_archivation")]
        public long? DateArchivation { get; set; }
        #endregion

        [JsonProperty("is_published")]
        public bool IsPublished { get; set; }

        [JsonProperty("is_sold")]
        public bool IsSold { get; set; }

        [JsonProperty("is_deleted")]
        public bool IsDeleted { get; set; }

        [JsonProperty("is_blocked")]
        public bool IsBlocked { get; set; }

        [JsonProperty("is_archived")]
        public bool IsArchived { get; set; }

        [JsonProperty("is_expiring")]
        public bool IsExpiring { get; set; }

        [JsonProperty("is_verified")]
        public bool IsVerified { get; set; }

        [JsonProperty("is_promoted")]
        public bool IsPromoted { get; set; }
    }
    public class Product
    {
        public Data data { get; set; }
        public int status { get; set; }
    }



    public class DateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.GetType() == typeof(long?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if(long.TryParse((reader.Value.ToString()), out long result))
            {
                return Tools.ConvertFromUnixTimestamp(result);
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if(value.GetType() == typeof(DateTime))
            {
                long? timestamp = Tools.ConvertToUnixTimestamp((DateTime)value);
                writer.WriteValue(timestamp);
            }
            else
            {
                long? timestamp = null;
                writer.WriteValue(timestamp);
            }
        }
    }



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
            //while (reader.Read())
            //{
            //    if (reader.Value != null)
            //    {
            //        Console.WriteLine("{0} : {1}", reader.TokenType, reader.Value);
            //    }
            //    else
            //    {
            //        Console.WriteLine("{0}", reader.TokenType);
            //    }
            //}
            JObject json = await JObject.LoadAsync(reader);

            Product product = JsonConvert.DeserializeObject<Product>(json.ToString());
            product.data.DateCreated = DateTime.Now;
            Console.WriteLine(JObject.Parse(JsonConvert.SerializeObject(product)));
        }
    }
}
