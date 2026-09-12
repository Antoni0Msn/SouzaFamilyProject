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

    /// <summary>
    /// Resposta de GET /title/{id}/details/?append_to_response=sources - traz o ID
    /// interno do Watchmode, o IMDb ID e as fontes de streaming numa única chamada
    /// (mais barato em créditos do que chamar /details e /sources separadamente).
    /// </summary>
    public class WatchmodeTitleDetailsDto
    {
        /// <summary>ID interno do Watchmode para este título (não confundir com o TmdbId).</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("tmdb_id")]
        public int? TmdbId { get; set; }

        [JsonPropertyName("tmdb_type")]
        public string? TmdbType { get; set; }

        [JsonPropertyName("sources")]
        public List<WatchmodeSourceDto> Sources { get; set; } = [];
    }

    /// <summary>Resposta de GET /list-titles - catálogo filtrado por source_ids/região.</summary>
    public class WatchmodeListTitlesResponseDto
    {
        [JsonPropertyName("titles")]
        public List<WatchmodeListTitleItemDto> Titles { get; set; } = [];

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    public class WatchmodeListTitleItemDto
    {
        /// <summary>ID interno do Watchmode.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("tmdb_id")]
        public int? TmdbId { get; set; }

        /// <summary>"movie" ou "tv".</summary>
        [JsonPropertyName("tmdb_type")]
        public string? TmdbType { get; set; }
    }
}