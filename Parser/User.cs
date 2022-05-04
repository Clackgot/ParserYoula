using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Parser
{

    /// <summary>
    /// Тип пользователя
    /// </summary>
    public enum UserType
    {
        [Description("Персона")]
        [EnumMember(Value = "person")]
        Person,
        [Description("Профессионал")]
        [EnumMember(Value = "b2b_professional")]
        B2BProfessional,
        [Description("Другое")]
        [EnumMember(Value = "other")]
        Other
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class User : BaseEntity
    {
        [JsonProperty("data")]
        public UserData Data { get; set; }
    }


    public class UserData : Data
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        #region Тип пользователя
        [JsonProperty("type")]
        [JsonConverter(typeof(UserTypeConverter))]
        public UserType Type { get; set; } 
        #endregion

        [JsonProperty("settings")]
        public Settings Settings { get; set; }


        #region Дата регистрации
        [JsonProperty("date_registered")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime DateRegistered { get; set; } 
        #endregion

        #region Дата последней авторизации
        [JsonProperty("last_auth_date")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime LastAuthDate { get; set; } 
        #endregion

        #region Подписки
        [JsonProperty("followers_cnt")]
        public int FollowersCnt { get; set; }

        [JsonProperty("following_cnt")]
        public int FollowingCnt { get; set; } 
        #endregion

        #region Рейтинг
        [JsonProperty("rating_mark")]
        public double RatingMark { get; set; }


        [JsonProperty("rating_mark_cnt")]
        public int RatingMarkCnt { get; set; } 
        #endregion

    }


    public class Settings
    {
        [JsonProperty("location")]
        public Location Location { get; set; }
    }
}