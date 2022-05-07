using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fix
{


    public class Product : JsonEntity
    {
        [JsonIgnore]
        public int Id { get; set; }
        #region Вложенные объекты
        [JsonProperty("owner")]
        public User Owner { get; set; }

        [JsonProperty("location")]
        public Location Location { get; set; } = new Location();

        [JsonProperty("images")]
        public IEnumerable<Image> Images { get; set; } = Array.Empty<Image>();
        #endregion

        #region Поля

        [JsonProperty("id")]
        public string IdString { get; set; }
        [JsonProperty("linked_id")]
        public string Linked_id { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("price")]
        public int Price { get; set; }
        [JsonProperty("price_text")]
        public string PriceText { get; set; }
        [JsonProperty("discount")]
        public int Discount { get; set; }
        [JsonProperty("discounted_price")]
        public int DiscountedPrice { get; set; }
        [JsonProperty("date_created")]
        public int? DateCreated { get; set; }
        [JsonProperty("date_updated")]
        public int? DateUpdated { get; set; }
        [JsonProperty("date_published")]
        public int? DatePublished { get; set; }
        [JsonProperty("date_sold")]
        public int? DateSold { get; set; }
        [JsonProperty("date_blocked")]
        public int? DateBlocked { get; set; }
        [JsonProperty("date_deleted")]
        public int? DateDeleted { get; set; }
        [JsonProperty("date_archivation")]
        public int? DateArchivation { get; set; }
        [JsonProperty("is_published")]
        public bool IsPublished { get; set; }
        [JsonProperty("is_sold")]
        public bool IsSold { get; set; }
        [JsonProperty("sold_mode")]
        public int SoldMode { get; set; }
        [JsonProperty("is_deleted")]
        public bool IsDeleted { get; set; }
        [JsonProperty("is_blocked")]
        public bool IsBlocked { get; set; }
        [JsonProperty("is_archived")]
        public bool IsArchived { get; set; }
        [JsonProperty("archive_mode")]
        public int ArchiveMode { get; set; }
        [JsonProperty("is_expiring")]
        public bool IsExpiring { get; set; }
        [JsonProperty("is_verified")]
        public bool IsVerified { get; set; }
        [JsonProperty("is_promoted")]
        public bool IsPromoted { get; set; }
        [JsonProperty("block_mode")]
        public int BlockMode { get; set; }
        [JsonProperty("block_type")]
        public int BlockType { get; set; }
        [JsonProperty("category")]
        public int Category { get; set; }
        [JsonProperty("subcategory")]
        public int Subcategory { get; set; }
        [JsonProperty("is_favorite")]
        public bool IsFavorite { get; set; }
        [JsonProperty("date_favorited")]
        public string DateFavorited { get; set; }
        [JsonProperty("views")]
        public int Views { get; set; }
        [JsonProperty("favorite_counter")]
        public int FavoriteCounter { get; set; }
        [JsonProperty("group_id")]
        public int GsroupId { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("url_branch")]
        public string UrlBranch { get; set; }
        [JsonProperty("short_url")]
        public string ShortUrl { get; set; }
        //[JsonProperty("share_url")]
        //public object? ShareUrl { get; set; }
        [JsonProperty("share_text")]
        public string ShareText { get; set; }
        [JsonProperty("contacts_visible")]
        public bool ContactsVisible { get; set; }
        //[JsonProperty("source")]
        //public object? Source { get; set; }
        //[JsonProperty("source_type")]
        //public object? SourceType { get; set; }
        [JsonProperty("price_with_discount_seller")]
        public int PriceWithDiscountSeller { get; set; }
        [JsonProperty("fire_promo_state")]
        public int FirePromoState { get; set; }
        [JsonProperty("p2p_call_rating_needed")]
        public bool P2pCallRatingNeeded { get; set; }
        [JsonProperty("is_master_exists")]
        public bool IsMasterExists { get; set; }
        [JsonProperty("is_default_photo")]
        public bool IsDefaultPhoto { get; set; }
        [JsonProperty("is_product_sale")]
        public bool? IsProductSale { get; set; }
        [JsonProperty("group_text")]
        public string GroupText { get; set; }
        [JsonProperty("engine")]
        public string Engine { get; set; }
        [JsonProperty("payment_available")]
        public bool PaymentAvailable { get; set; }
        [JsonProperty("delivery_available")]
        public bool DeliveryAvailable { get; set; }
        [JsonProperty("is_paid_ad")]
        public bool IsPaidAd { get; set; }
        [JsonProperty("promotion_type")]
        public int PromotionType { get; set; }
        [JsonProperty("delivery_type")]
        public int DeliveryType { get; set; }
        [JsonProperty("distance_text")]
        public string DistanceText { get; set; }
        [JsonProperty("distance")]
        public int Distance { get; set; }
        [JsonProperty("offset")]
        public int Offset { get; set; }
        [JsonProperty("inactive_text")]
        public string InactiveText { get; set; }
        #endregion
    }

    public class Location : JsonEntity
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty("latitude")]
        public float Latitude { get; set; }
        [JsonProperty("Longitude")]
        public float Longitude { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("city")]
        public string City { get; set; }
        [JsonProperty("city_name")]
        public string CityName { get; set; }
    }

    public class Image : JsonEntity
    {
        [JsonIgnore]
        public int Id { get; set; }
        [JsonProperty("id")]
        public string IdString { get; set; }
        [JsonProperty("num")]
        public int Num { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
    }

}
