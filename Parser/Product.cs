using Newtonsoft.Json;
using System;

namespace Parser
{
    public class Product
    {
        public Data data { get; set; }
        public int status { get; set; }
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
}
