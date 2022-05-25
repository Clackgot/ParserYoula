using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parser
{
    public class City : JsonEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("coords")]
        public Coordinate Coordinate { get; set; }

        [JsonProperty("name_pp")]
        public string NamePp { get; set; }
        [JsonProperty("parent_id")]
        public string ParentId { get; set; } = string.Empty;
        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;
        [JsonProperty("level")]
        public int? Level { get; set; }
        [JsonProperty("products_cnt")]
        public int? ProductsCount { get; set; }
        [JsonProperty("top_score")]
        public int? TopScore { get; set; }
        [JsonProperty("code_rb")]
        public int? CodeRb { get; set; }

    }

    public class Coordinate : JsonEntity
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }
        [JsonProperty("longitude")]
        public double Longitude { get; set; }
        [JsonIgnore]
        public string CityId { get; set; } = null!;
    }


    public class CitiesResponse
    {
        [JsonProperty("data")]
        public City[] Citites { get; set; }
        [JsonProperty("status")]
        public int Status { get; set; }
        [JsonProperty("detail")]
        public string Detail { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
    }


}
