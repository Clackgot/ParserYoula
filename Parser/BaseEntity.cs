using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Parser
{
    public abstract class BaseEntity
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        public override string ToString()
        {
            return JObject.Parse(JsonConvert.SerializeObject(this)).ToString();
        }
    }

    public abstract class Data
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Location
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

}