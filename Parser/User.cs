using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Parser
{


    public class User : BaseEntity
    {
        [JsonProperty("data")]
        public UserData Data { get; set; }
    }


    public class UserData : Data
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("settings")]
        public Settings Settings { get; set; }

    }

    public class Settings
    {
        [JsonProperty("location")]
        public Location Location { get; set; }
    }
}