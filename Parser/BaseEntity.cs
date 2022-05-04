using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Parser
{
    public abstract class JsonEntity
    {
        public override string ToString()
        {
            return JObject.Parse(JsonConvert.SerializeObject(this)).ToString();
        }
    }

    public abstract class BaseEntity : JsonEntity
    {
        [JsonProperty("status")]
        public int Status { get; set; }
    }

    public abstract class Data : JsonEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Location : JsonEntity
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("longitude")]
        public double? Longitude { get; set; }

    }

}