using Newtonsoft.Json;
using System;

namespace Parser
{




    public class Product : BaseEntity
    {
        [JsonProperty("data")]
        public ProductData Data { get; set; }
    }

    public class ProductData : Data
    {
        [JsonProperty("location")]
        public Location Location { get; set; }


        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        #region Даты
        [JsonProperty("date_created")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime? DateCreated { get; set; }

        [JsonProperty("date_updated")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime? DateUpdated { get; set; }

        [JsonProperty("date_published")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime? DatePublished { get; set; }

        [JsonProperty("date_sold")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime? DateSold { get; set; }

        [JsonProperty("date_blocked")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime? DateBlocked { get; set; }

        [JsonProperty("date_deleted")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime? DateDeleted { get; set; }

        [JsonProperty("date_archivation")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime? DateArchivation { get; set; }

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
