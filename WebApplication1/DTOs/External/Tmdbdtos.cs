using System.Text.Json.Serialization;

namespace WebApplication1.DTOs.External
{
    public class TmdbPagedResponse<T>
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("results")]
        public List<T> Results { get; set; } = [];

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }
    }

    public class TmdbMovieDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("vote_average")]
        public double? VoteAverage { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        /// <summary>Formato do endpoint /movie/popular (lista de IDs).</summary>
        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; } = [];

        /// <summary>Formato do endpoint /movie/{id} (lista de objetos completos).</summary>
        [JsonPropertyName("genres")]
        public List<TmdbGenreDto>? Genres { get; set; }

        /// <summary>Usa qualquer um dos dois formatos que tiver vindo preenchido.</summary>
        [JsonIgnore]
        public List<int> ResolvedGenreIds => GenreIds.Count > 0
            ? GenreIds
            : Genres?.Select(g => g.Id).ToList() ?? [];
    }

    public class TmdbTvDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("vote_average")]
        public double? VoteAverage { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }

        /// <summary>Formato do endpoint /tv/popular (lista de IDs).</summary>
        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; } = [];

        /// <summary>Formato do endpoint /tv/{id} (lista de objetos completos).</summary>
        [JsonPropertyName("genres")]
        public List<TmdbGenreDto>? Genres { get; set; }

        [JsonIgnore]
        public List<int> ResolvedGenreIds => GenreIds.Count > 0
            ? GenreIds
            : Genres?.Select(g => g.Id).ToList() ?? [];
    }

    public class TmdbGenreDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class TmdbGenreListDto
    {
        [JsonPropertyName("genres")]
        public List<TmdbGenreDto> Genres { get; set; } = [];
    }
}