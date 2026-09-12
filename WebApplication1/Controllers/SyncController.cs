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
        private readonly SyncService _syncService;
        private readonly WatchmodeService _watchmode;
        private readonly SyncOptions _syncOptions;

        public SyncController(SyncService syncService, WatchmodeService watchmode, IOptions<SyncOptions> syncOptions)
        {
            _syncService = syncService;
            _watchmode = watchmode;
            _syncOptions = syncOptions.Value;
        }

        [HttpPost("movies")]
        public async Task<IActionResult> SyncMovies([FromQuery] int pages = 1, CancellationToken ct = default)
        {
            if (!IsAuthorized(Request)) return Unauthorized();

            var log = await _syncService.SyncMoviesAsync(pages, ct);
            return Ok(log);
        }

        [HttpPost("series")]
        public async Task<IActionResult> SyncSeries([FromQuery] int pages = 1, CancellationToken ct = default)
        {
            if (!IsAuthorized(Request)) return Unauthorized();

            var log = await _syncService.SyncSeriesAsync(pages, ct);
            return Ok(log);
        }

        /// <summary>
        /// Não salva nada no banco — só mostra exatamente o que o Watchmode devolve
        /// pra um título (nome do provider, tipo, região etc). Útil pra descobrir o
        /// nome exato a colocar em Watchmode:AllowedProviders.
        /// Ex: GET /api/sync/debug/sources?tmdbId=550&isMovie=true (Fight Club)
        /// </summary>
        [HttpGet("debug/sources")]
        public async Task<IActionResult> DebugSources(
            [FromQuery] int tmdbId, [FromQuery] bool isMovie = true, CancellationToken ct = default)
        {
            if (!IsAuthorized(Request)) return Unauthorized();

            var sources = await _watchmode.GetSourcesByTmdbIdAsync(tmdbId, isMovie, ct: ct);
            return Ok(sources);
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