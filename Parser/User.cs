using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Parser
{

    /// <summary>
    /// Тип пользователя
    /// </summary>
    public enum UserType
    {
        [EnumMember(Value = "person")]
        Person,
        [EnumMember(Value = "b2b_professional")]
        B2BProfessional,
    }


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
        [JsonConverter(typeof(StringEnumConverter))]
        public UserType Type { get; set; }

        [JsonProperty("settings")]
        public Settings Settings { get; set; }

    }

    public class Settings
    {
        [JsonProperty("location")]
        public Location Location { get; set; }
    }
}