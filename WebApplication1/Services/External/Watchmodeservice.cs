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
        /// Lista títulos direto do catálogo do Watchmode filtrado pelos streamings configurados
        /// (source_ids) — muito mais eficiente que perguntar "isso está popular?" pro TMDB e só
        /// depois checar se está no seu streaming: aqui você já pede "o que está no Netflix/
        /// Prime/Disney+ no Brasil?" e recebe só o que interessa.
        /// </summary>
        public async Task<WatchmodeListTitlesResponseDto> ListTitlesBySourcesAsync(
            List<int> sourceIds, string? regions = null, int page = 1, int limit = 250, CancellationToken ct = default)
        {
            var region = regions ?? _options.DefaultRegion;
            var sourceIdsParam = string.Join(",", sourceIds);
            var url = $"list-titles/?source_ids={sourceIdsParam}&regions={region}&sort_by=popularity_desc&page={page}&limit={limit}";

            var response = await GetWithRetryAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<WatchmodeListTitlesResponseDto>(cancellationToken: ct);
            return result ?? new WatchmodeListTitlesResponseDto();
        }

        /// <summary>
        /// Busca detalhes (incluindo o ID interno do Watchmode e o IMDb ID) e as fontes de
        /// streaming de um título, numa única chamada — usa append_to_response=sources em vez
        /// de bater em /details e /sources separadamente, o que custaria mais créditos.
        /// Aceita diretamente o ID do TMDB (não precisamos buscar o ID do Watchmode antes).
        /// </summary>
        public async Task<WatchmodeTitleDetailsDto?> GetTitleDetailsWithSourcesAsync(
            int tmdbId, bool isMovie, string? regions = null, CancellationToken ct = default)
        {
            var type = isMovie ? "movie" : "tv";
            var region = regions ?? _options.DefaultRegion;
            var url = $"title/{type}-{tmdbId}/details/?append_to_response=sources&regions={region}";

            var response = await GetWithRetryAsync(url, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Título ainda não catalogado pelo Watchmode - não é um erro fatal,
                // só significa que não temos "onde assistir" pra ele ainda.
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<WatchmodeTitleDetailsDto>(cancellationToken: ct);
        }

        /// <summary>
        /// GET com retry automático quando o Watchmode devolve 429 (rate limit). Respeita o
        /// header Retry-After se ele vier; senão usa backoff exponencial (2s, 4s, 8s...).
        /// Depois de esgotar as tentativas, devolve a resposta (com erro) pro chamador decidir.
        /// </summary>
        private async Task<HttpResponseMessage> GetWithRetryAsync(string url, CancellationToken ct, int maxRetries = 5)
        {
            var attempt = 0;

            while (true)
            {
                var response = await _httpClient.GetAsync(url, ct);

                if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= maxRetries)
                {
                    return response;
                }

                attempt++;
                var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, ct);
            }
        }
    }
}