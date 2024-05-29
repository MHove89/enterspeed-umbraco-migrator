using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Enterspeed.Migrator.Models.Response
{
    public class Meta
    {
        [JsonPropertyName("Status")]
        public int Status { get; set; }

        [JsonPropertyName("Redirect")]
        public object Redirect { get; set; }
    }

    public class Self
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("sourceId")]
        public string SourceId { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("self")]
        public Self Self { get; set; }

        [JsonPropertyName("children")]
        public List<Item> Children { get; set; }
    }

    public class Views
    {
        [JsonPropertyName("navigation")]
        public Item Navigation { get; set; }
    }

    public class EnterspeedResponse
    {
        [JsonPropertyName("Meta")]
        public Meta Meta { get; set; }

        [JsonPropertyName("Route")]
        public object Route { get; set; }

        [JsonPropertyName("Views")]
        public Views Views { get; set; }
    }
}