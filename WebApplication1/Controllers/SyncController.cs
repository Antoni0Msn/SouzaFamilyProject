using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApplication1.Configurations;
using WebApplication1.Services;
using WebApplication1.Services.External;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        // Tetos de segurança: mesmo que alguém passe um valor enorme na query string,
        // o gasto de créditos por chamada fica limitado.
        private const int MaxPagesLimit = 20;
        private const int MaxLimitPerPage = 250;   // máximo aceito pelo /list-titles
        private const int MaxNewTitlesLimit = 500;

        private readonly SyncService _syncService;
        private readonly WatchmodeService _watchmode;
        private readonly SyncOptions _syncOptions;

        public SyncController(SyncService syncService, WatchmodeService watchmode, IOptions<SyncOptions> syncOptions)
        {
            _syncService = syncService;
            _watchmode = watchmode;
            _syncOptions = syncOptions.Value;
        }

        /// <summary>
        /// Fluxo 1. Por padrão pula filmes que já estão no banco com providers (0 créditos).
        /// forceRefresh=true reconsulta tudo - use só pontualmente.
        /// </summary>
        [HttpPost("movies")]
        public async Task<IActionResult> SyncMovies(
            [FromQuery] int pages = 1, [FromQuery] bool forceRefresh = false, CancellationToken ct = default)
        {
            if (!IsAuthorized(Request)) return Unauthorized();

            pages = Math.Clamp(pages, 1, MaxPagesLimit);

            var log = await _syncService.SyncMoviesAsync(pages, ct, forceRefresh);
            return Ok(log);
        }

        /// <summary>
        /// Fluxo 1. Por padrão pula séries que já estão no banco com providers (0 créditos).
        /// </summary>
        [HttpPost("series")]
        public async Task<IActionResult> SyncSeries(
            [FromQuery] int pages = 1, [FromQuery] bool forceRefresh = false, CancellationToken ct = default)
        {
            if (!IsAuthorized(Request)) return Unauthorized();

            pages = Math.Clamp(pages, 1, MaxPagesLimit);

            var log = await _syncService.SyncSeriesAsync(pages, ct, forceRefresh);
            return Ok(log);
        }

        /// <summary>
        /// Fluxo recomendado para coletar volume: parte do catálogo do Watchmode já
        /// filtrado pelos streamings configurados (Watchmode:AllowedSourceIds).
        /// Títulos já resolvidos no banco são pulados sem gastar crédito.
        /// maxNewTitles = teto de chamadas pagas (por título) nesta execução; o sync
        /// para assim que atinge esse número. Comece com um valor baixo (ex: 50).
        /// </summary>
        [HttpPost("catalog")]
        public async Task<IActionResult> SyncCatalog(
            [FromQuery] int maxPages = 10,
            [FromQuery] int limitPerPage = 250,
            [FromQuery] int maxNewTitles = 100,
            [FromQuery] bool forceRefresh = false,
            CancellationToken ct = default)
        {
            if (!IsAuthorized(Request)) return Unauthorized();

            maxPages = Math.Clamp(maxPages, 1, MaxPagesLimit);
            limitPerPage = Math.Clamp(limitPerPage, 1, MaxLimitPerPage);
            maxNewTitles = Math.Clamp(maxNewTitles, 1, MaxNewTitlesLimit);

            var log = await _syncService.SyncFromWatchmodeCatalogAsync(
                maxPages, limitPerPage, ct, forceRefresh, maxNewTitles);

            // Se já havia outro sync rodando, o service devolve esse aviso sem gastar nada.
            if (!log.Success && log.RecordsProcessed == 0 &&
                log.ErrorMessage?.StartsWith("Já existe um sync em andamento") == true)
            {
                return Conflict(log);
            }

            return Ok(log);
        }

        /// <summary>
        /// Não salva nada no banco — só mostra exatamente o que o Watchmode devolve
        /// pra um título (ID interno do Watchmode, IMDb ID, e cada provider com nome,
        /// tipo, região etc). ATENÇÃO: cada chamada aqui gasta crédito do Watchmode.
        /// Ex: GET /api/sync/debug/sources?tmdbId=550&isMovie=true (Fight Club)
        /// </summary>
        [HttpGet("debug/sources")]
        public async Task<IActionResult> DebugSources(
            [FromQuery] int tmdbId, [FromQuery] bool isMovie = true, CancellationToken ct = default)
        {
            if (!IsAuthorized(Request)) return Unauthorized();

            var details = await _watchmode.GetTitleDetailsWithSourcesAsync(tmdbId, isMovie, ct: ct);
            return Ok(details);
        }

        // Proteção simples via header enquanto a autenticação por roles (admin) ainda não
        // está implementada. Quando o AuthController/Identity com roles estiver pronto,
        // troque isso por [Authorize(Roles = "Admin")].
        private bool IsAuthorized(HttpRequest request)
        {
            if (string.IsNullOrEmpty(_syncOptions.ApiKey)) return false;
            return request.Headers.TryGetValue("X-Sync-Key", out var provided) && provided == _syncOptions.ApiKey;
        }
    }
}
