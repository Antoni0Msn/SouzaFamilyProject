using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WebApplication1.Configurations;
using WebApplication1.DTOs.External;

namespace WebApplication1.Services.External
{
    /// <summary>
    /// Encapsula todas as chamadas à API do Watchmode. Nenhum outro lugar do projeto
    /// deve chamar o Watchmode diretamente — sempre passe por aqui.
    /// </summary>
    public class WatchmodeService
    {
        private readonly HttpClient _httpClient;
        private readonly WatchmodeOptions _options;

        public WatchmodeService(HttpClient httpClient, IOptions<WatchmodeOptions> options)
        {
            _options = options.Value;
            _httpClient = httpClient;

            if (_httpClient.BaseAddress is null)
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }

            if (!_httpClient.DefaultRequestHeaders.Contains("X-API-Key"))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
            }
        }

        /// <summary>
        /// Busca as fontes de streaming de um título usando diretamente o ID do TMDB —
        /// o Watchmode aceita "movie-{tmdbId}" ou "tv-{tmdbId}" no lugar do próprio ID dele,
        /// então não precisamos fazer uma busca prévia por ID do Watchmode.
        /// </summary>
        public async Task<List<WatchmodeSourceDto>> GetSourcesByTmdbIdAsync(
            int tmdbId, bool isMovie, string? regions = null, CancellationToken ct = default)
        {
            var type = isMovie ? "movie" : "tv";
            var region = regions ?? _options.DefaultRegion;
            var url = $"title/{type}-{tmdbId}/sources/?regions={region}";

            var response = await _httpClient.GetAsync(url, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Título ainda não catalogado pelo Watchmode - não é um erro fatal,
                // só significa que não temos "onde assistir" pra ele ainda.
                return [];
            }

            response.EnsureSuccessStatusCode();

            var sources = await response.Content.ReadFromJsonAsync<List<WatchmodeSourceDto>>(cancellationToken: ct);
            return sources ?? [];
        }
    }
}