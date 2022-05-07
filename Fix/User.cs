using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fix
{

    public class UserResponse : JsonEntity
    {
        [JsonProperty("data")]
        public User User { get; set; }
        [JsonProperty("status")]
        public int Status { get; set; }
        [JsonProperty("detail")]
        public string Detail { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public class User : JsonEntity
    {
        public string id { get; set; }
        public string type { get; set; }
        public string linked_id { get; set; }
        public string name { get; set; }
        public Image image { get; set; }
        public object phone { get; set; }
        public object social { get; set; }
        public bool isOnline { get; set; }
        public string onlineText { get; set; }
        public string online_text_detailed { get; set; }
        public Settings settings { get; set; }
        public object short_name { get; set; }
        public string share_link { get; set; }
        public string share_text { get; set; }
        public int date_registered { get; set; }
        public bool is_blocked { get; set; }
        public bool is_shop { get; set; }
        public bool is_verified { get; set; }
        public string common_channel { get; set; }
        public bool experienced_seller { get; set; }
        public object options { get; set; }
        public Store store { get; set; }
        public Verification verification { get; set; }
        public object actor_id { get; set; }
        public bool b2b_with_manager { get; set; }
        public int blacklist_status { get; set; }
        public int blacklist_cnt { get; set; }
        public int followers_cnt { get; set; }
        public int following_cnt { get; set; }
        public float rating_mark { get; set; }
        public int rating_mark_cnt { get; set; }
        public int orders_cnt { get; set; }
        public int last_auth_date { get; set; }
        public int prods_active_cnt { get; set; }
        public int prods_sold_cnt { get; set; }
        public int orders_seller_cnt { get; set; }
        public int orders_buyer_cnt { get; set; }
        public int contacts_active_cnt { get; set; }
        public Rating_Detailed[] rating_detailed { get; set; }
        public object subscription_date_added { get; set; }
        public bool is_subscribed { get; set; }
    }

    public class Settings : JsonEntity
    {
        public bool display_chat { get; set; }
        public int locale { get; set; }
        public bool vk_chat_messages_enabled { get; set; }
        public Location location { get; set; }
        public bool display_phone { get; set; }
    }

    public class Store : JsonEntity
    {
        public string id { get; set; }
        public string title { get; set; }
        public Logo logo { get; set; }
    }

    public class Logo : JsonEntity
    {
        public string id { get; set; }
        public string url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public int num { get; set; }
    }

    public class Verification : JsonEntity
    {
        public int type { get; set; }
        public int vk_friends_count { get; set; }
        public int required_type { get; set; }
    }

    public class Rating_Detailed : JsonEntity
    {
        public string type { get; set; }
        public int mark { get; set; }
        public string title { get; set; }
    }

}
