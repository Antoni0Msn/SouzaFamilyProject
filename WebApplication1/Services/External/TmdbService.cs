using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WebApplication1.Configurations;
using WebApplication1.DTOs.External;

namespace WebApplication1.Services.External
{
    /// <summary>
    /// Encapsula todas as chamadas à API do TMDB. Nenhum outro lugar do projeto
    /// deve chamar o TMDB diretamente — sempre passe por aqui.
    /// </summary>
    public class TmdbService
    {
        private readonly HttpClient _httpClient;
        private readonly TmdbOptions _options;

        public TmdbService(HttpClient httpClient, IOptions<TmdbOptions> options)
        {
            _options = options.Value;
            _httpClient = httpClient;

            if (_httpClient.BaseAddress is null)
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }
        }

        public async Task<TmdbPagedResponse<TmdbMovieDto>> GetPopularMoviesAsync(int page = 1, CancellationToken ct = default)
        {
            var url = $"movie/popular?api_key={_options.ApiKey}&language={_options.Language}&page={page}";
            var response = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbMovieDto>>(url, ct);
            return response ?? new TmdbPagedResponse<TmdbMovieDto>();
        }

        public async Task<TmdbPagedResponse<TmdbTvDto>> GetPopularTvAsync(int page = 1, CancellationToken ct = default)
        {
            var url = $"tv/popular?api_key={_options.ApiKey}&language={_options.Language}&page={page}";
            var response = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbTvDto>>(url, ct);
            return response ?? new TmdbPagedResponse<TmdbTvDto>();
        }

        /// <summary>Mapa TmdbGenreId -> Nome do gênero, para filmes.</summary>
        public async Task<Dictionary<int, string>> GetMovieGenreMapAsync(CancellationToken ct = default)
        {
            var url = $"genre/movie/list?api_key={_options.ApiKey}&language={_options.Language}";
            var response = await _httpClient.GetFromJsonAsync<TmdbGenreListDto>(url, ct);
            return (response?.Genres ?? []).ToDictionary(g => g.Id, g => g.Name);
        }

        /// <summary>Mapa TmdbGenreId -> Nome do gênero, para séries.</summary>
        public async Task<Dictionary<int, string>> GetTvGenreMapAsync(CancellationToken ct = default)
        {
            var url = $"genre/tv/list?api_key={_options.ApiKey}&language={_options.Language}";
            var response = await _httpClient.GetFromJsonAsync<TmdbGenreListDto>(url, ct);
            return (response?.Genres ?? []).ToDictionary(g => g.Id, g => g.Name);
        }

        public string? BuildPosterUrl(string? path) =>
            string.IsNullOrEmpty(path) ? null : $"{_options.ImageBaseUrl}/w500{path}";

        public string? BuildBackdropUrl(string? path) =>
            string.IsNullOrEmpty(path) ? null : $"{_options.ImageBaseUrl}/original{path}";
    }
}