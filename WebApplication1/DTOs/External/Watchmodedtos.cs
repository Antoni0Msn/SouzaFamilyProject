using System.Text.Json.Serialization;

namespace WebApplication1.DTOs.External
{
    public class WatchmodeSourceDto
    {
        [JsonPropertyName("source_id")]
        public int SourceId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>sub / rent / buy / free / tve</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("region")]
        public string Region { get; set; } = string.Empty;

        [JsonPropertyName("web_url")]
        public string? WebUrl { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }
    }
}