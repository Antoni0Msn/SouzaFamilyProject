using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApplication1.Configurations;
using WebApplication1.Data;
using WebApplication1.DTOs.External;
using WebApplication1.Models;
using WebApplication1.Services.External;

namespace WebApplication1.Services
{
    /// <summary>
    /// Orquestra a sincronização: busca dados no TMDB (metadados) e no Watchmode
    /// (onde assistir), e grava/atualiza tudo no Supabase via AppDbContext.
    /// Controllers nunca devem chamar TmdbService/WatchmodeService diretamente —
    /// sempre atravessando este serviço.
    /// </summary>
    public class SyncService
    {
        private readonly AppDbContext _db;
        private readonly TmdbService _tmdb;
        private readonly WatchmodeService _watchmode;
        private readonly WatchmodeOptions _watchmodeOptions;
        private readonly ILogger<SyncService> _logger;

        public SyncService(
            AppDbContext db,
            TmdbService tmdb,
            WatchmodeService watchmode,
            IOptions<WatchmodeOptions> watchmodeOptions,
            ILogger<SyncService> logger)
        {
            _db = db;
            _tmdb = tmdb;
            _watchmode = watchmode;
            _watchmodeOptions = watchmodeOptions.Value;
            _logger = logger;
        }

        // =====================================================================
        // FLUXO 1 (original): parte do TMDB "populares", filtra pelo Watchmode.
        // Simples, mas desperdiça chamadas em títulos que não estão nos seus
        // streamings. Bom para um sync incremental leve.
        // =====================================================================

        public async Task<SyncLog> SyncMoviesAsync(int pages = 1, CancellationToken ct = default)
        {
            var log = new SyncLog { Source = "TMDB+Watchmode", SyncType = "Movies", StartedAt = DateTime.UtcNow };
            var seenProviders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = 0;

            try
            {
                var genreMap = await _tmdb.GetMovieGenreMapAsync(ct);

                for (var page = 1; page <= pages; page++)
                {
                    var popular = await _tmdb.GetPopularMoviesAsync(page, ct);

                    foreach (var movie in popular.Results)
                    {
                        log.RecordsProcessed++;

                        try
                        {
                            var details = await _watchmode.GetTitleDetailsWithSourcesAsync(movie.Id, isMovie: true, ct: ct);
                            var sources = details?.Sources ?? [];
                            foreach (var s in sources) seenProviders.Add(s.Name);

                            var allowedSources = FilterAllowedSources(sources);
                            if (allowedSources.Count == 0) continue; // não está nos streamings cobertos

                            var (title, created) = await UpsertTitleFromMovieAsync(movie, genreMap, ct);
                            title.WatchmodeId = details?.Id;
                            title.ImdbId = details?.ImdbId;
                            if (created) log.RecordsCreated++; else log.RecordsUpdated++;

                            await UpsertProvidersAsync(title, allowedSources, ct);
                        }
                        catch (Exception itemEx)
                        {
                            // Um item com problema (rate limit, dado malformado etc.) não deve
                            // derrubar a página inteira - loga e segue pro próximo.
                            errors++;
                            _logger.LogWarning(itemEx, "Falha ao processar o filme TMDB id={TmdbId}", movie.Id);
                        }
                    }

                    await _db.SaveChangesAsync(ct);

                    if (page >= popular.TotalPages) break;
                }

                log.Success = errors == 0;
                if (errors > 0) log.ErrorMessage = $"{errors} item(ns) falharam durante o processamento (ver logs).";
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Falha ao sincronizar filmes populares (TMDB/Watchmode)");
            }
            finally
            {
                log.FinishedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Sync de filmes: providers vistos nesta execução: {Providers}",
                    seenProviders.Count > 0 ? string.Join(", ", seenProviders) : "(nenhum)");
                _db.SyncLogs.Add(log);
                await _db.SaveChangesAsync(ct);
            }

            return log;
        }

        public async Task<SyncLog> SyncSeriesAsync(int pages = 1, CancellationToken ct = default)
        {
            var log = new SyncLog { Source = "TMDB+Watchmode", SyncType = "Series", StartedAt = DateTime.UtcNow };
            var seenProviders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = 0;

            try
            {
                var genreMap = await _tmdb.GetTvGenreMapAsync(ct);

                for (var page = 1; page <= pages; page++)
                {
                    var popular = await _tmdb.GetPopularTvAsync(page, ct);

                    foreach (var tv in popular.Results)
                    {
                        log.RecordsProcessed++;

                        try
                        {
                            var details = await _watchmode.GetTitleDetailsWithSourcesAsync(tv.Id, isMovie: false, ct: ct);
                            var sources = details?.Sources ?? [];
                            foreach (var s in sources) seenProviders.Add(s.Name);

                            var allowedSources = FilterAllowedSources(sources);
                            if (allowedSources.Count == 0) continue; // não está nos streamings cobertos

                            var (title, created) = await UpsertTitleFromTvAsync(tv, genreMap, ct);
                            title.WatchmodeId = details?.Id;
                            title.ImdbId = details?.ImdbId;
                            if (created) log.RecordsCreated++; else log.RecordsUpdated++;

                            await UpsertProvidersAsync(title, allowedSources, ct);
                        }
                        catch (Exception itemEx)
                        {
                            errors++;
                            _logger.LogWarning(itemEx, "Falha ao processar a série TMDB id={TmdbId}", tv.Id);
                        }
                    }

                    await _db.SaveChangesAsync(ct);

                    if (page >= popular.TotalPages) break;
                }

                log.Success = errors == 0;
                if (errors > 0) log.ErrorMessage = $"{errors} item(ns) falharam durante o processamento (ver logs).";
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Falha ao sincronizar séries populares (TMDB/Watchmode)");
            }
            finally
            {
                log.FinishedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Sync de séries: providers vistos nesta execução: {Providers}",
                    seenProviders.Count > 0 ? string.Join(", ", seenProviders) : "(nenhum)");
                _db.SyncLogs.Add(log);
                await _db.SaveChangesAsync(ct);
            }

            return log;
        }

        // =====================================================================
        // FLUXO 2 (recomendado para volume): parte do catálogo do Watchmode já
        // filtrado pelos seus streamings (AllowedSourceIds), depois busca os
        // metadados completos no TMDB. Não desperdiça chamadas em títulos fora
        // dos seus streamings, e cobre MUITO mais do catálogo real deles.
        // =====================================================================

        public async Task<SyncLog> SyncFromWatchmodeCatalogAsync(int maxPages = 10, int limitPerPage = 250, CancellationToken ct = default)
        {
            var log = new SyncLog { Source = "Watchmode+TMDB", SyncType = "Catalog", StartedAt = DateTime.UtcNow };
            var errors = 0;

            try
            {
                if (_watchmodeOptions.AllowedSourceIds.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Watchmode:AllowedSourceIds não está configurado. Descubra os source_id dos seus " +
                        "streamings via GET /api/sync/debug/sources e configure a lista antes de rodar este sync.");
                }

                var movieGenreMap = await _tmdb.GetMovieGenreMapAsync(ct);
                var tvGenreMap = await _tmdb.GetTvGenreMapAsync(ct);

                for (var page = 1; page <= maxPages; page++)
                {
                    var list = await _watchmode.ListTitlesBySourcesAsync(
                        _watchmodeOptions.AllowedSourceIds, page: page, limit: limitPerPage, ct: ct);

                    if (list.Titles.Count == 0) break;

                    foreach (var item in list.Titles)
                    {
                        log.RecordsProcessed++;

                        if (item.TmdbId is null)
                        {
                            // Título sem correspondência no TMDB - não temos como buscar metadados dele.
                            continue;
                        }

                        var isMovie = string.Equals(item.TmdbType, "movie", StringComparison.OrdinalIgnoreCase);

                        try
                        {
                            var details = await _watchmode.GetTitleDetailsWithSourcesAsync(item.TmdbId.Value, isMovie, ct: ct);
                            var allowedSources = FilterAllowedSources(details?.Sources ?? []);

                            if (allowedSources.Count == 0)
                            {
                                // Segurança extra - teoricamente não deveria acontecer já que o
                                // /list-titles já filtrou por source_ids, mas providers "via Amazon
                                // Prime" etc podem ter nomes que não batem com AllowedProviders.
                                continue;
                            }

                            Title title;
                            bool created;

                            if (isMovie)
                            {
                                var movieDto = await _tmdb.GetMovieDetailsAsync(item.TmdbId.Value, ct);
                                if (movieDto is null) continue;
                                (title, created) = await UpsertTitleFromMovieAsync(movieDto, movieGenreMap, ct);
                            }
                            else
                            {
                                var tvDto = await _tmdb.GetTvDetailsAsync(item.TmdbId.Value, ct);
                                if (tvDto is null) continue;
                                (title, created) = await UpsertTitleFromTvAsync(tvDto, tvGenreMap, ct);
                            }

                            title.WatchmodeId = details?.Id ?? item.Id;
                            title.ImdbId = details?.ImdbId ?? item.ImdbId;
                            if (created) log.RecordsCreated++; else log.RecordsUpdated++;

                            await UpsertProvidersAsync(title, allowedSources, ct);
                        }
                        catch (Exception itemEx)
                        {
                            errors++;
                            _logger.LogWarning(itemEx, "Falha ao processar título TMDB id={TmdbId} (Watchmode id={WatchmodeId})", item.TmdbId, item.Id);
                        }
                    }

                    await _db.SaveChangesAsync(ct);

                    if (page >= list.TotalPages) break;
                }

                log.Success = errors == 0;
                if (errors > 0) log.ErrorMessage = $"{errors} item(ns) falharam durante o processamento (ver logs).";
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Falha ao sincronizar catálogo a partir do Watchmode");
            }
            finally
            {
                log.FinishedAt = DateTime.UtcNow;
                _db.SyncLogs.Add(log);
                await _db.SaveChangesAsync(ct);
            }

            return log;
        }

        // =====================================================================
        // Helpers compartilhados pelos dois fluxos
        // =====================================================================

        /// <summary>
        /// O TMDB devolve datas simples ("2021-05-10", sem timezone). O Npgsql exige que todo
        /// DateTime gravado em coluna "timestamp with time zone" tenha Kind=Utc - um DateTime
        /// comum vindo de DateTime.TryParse tem Kind=Unspecified e quebra o SaveChanges.
        /// </summary>
        private static DateTime? TryParseUtcDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var date)
                ? date
                : null;
        }

        private async Task<(Title Title, bool Created)> UpsertTitleFromMovieAsync(
            TmdbMovieDto movie, Dictionary<int, string> genreMap, CancellationToken ct)
        {
            var title = await _db.Titles
                .Include(t => t.Genres).ThenInclude(tg => tg.Genre)
                .FirstOrDefaultAsync(t => t.TmdbId == movie.Id && t.Type == "Movie", ct);

            var created = title is null;
            if (title is null)
            {
                title = new Title { TmdbId = movie.Id, CreatedAt = DateTime.UtcNow };
                _db.Titles.Add(title);
            }

            title.Name = movie.Title;
            title.Type = "Movie";
            title.Description = movie.Overview;
            title.PosterUrl = _tmdb.BuildPosterUrl(movie.PosterPath);
            title.BackdropUrl = _tmdb.BuildBackdropUrl(movie.BackdropPath);
            title.Rating = movie.VoteAverage;
            title.ReleaseDate = TryParseUtcDate(movie.ReleaseDate);
            title.UpdatedAt = DateTime.UtcNow;

            await AttachGenresAsync(title, movie.ResolvedGenreIds, genreMap, ct);

            return (title, created);
        }

        private async Task<(Title Title, bool Created)> UpsertTitleFromTvAsync(
            TmdbTvDto tv, Dictionary<int, string> genreMap, CancellationToken ct)
        {
            var title = await _db.Titles
                .Include(t => t.Genres).ThenInclude(tg => tg.Genre)
                .FirstOrDefaultAsync(t => t.TmdbId == tv.Id && t.Type == "Series", ct);

            var created = title is null;
            if (title is null)
            {
                title = new Title { TmdbId = tv.Id, CreatedAt = DateTime.UtcNow };
                _db.Titles.Add(title);
            }

            title.Name = tv.Name;
            title.Type = "Series";
            title.Description = tv.Overview;
            title.PosterUrl = _tmdb.BuildPosterUrl(tv.PosterPath);
            title.BackdropUrl = _tmdb.BuildBackdropUrl(tv.BackdropPath);
            title.Rating = tv.VoteAverage;
            title.ReleaseDate = TryParseUtcDate(tv.FirstAirDate);
            title.UpdatedAt = DateTime.UtcNow;

            await AttachGenresAsync(title, tv.ResolvedGenreIds, genreMap, ct);

            return (title, created);
        }

        private async Task AttachGenresAsync(
            Title title, List<int> tmdbGenreIds, Dictionary<int, string> genreMap, CancellationToken ct)
        {
            foreach (var tmdbGenreId in tmdbGenreIds)
            {
                if (!genreMap.TryGetValue(tmdbGenreId, out var genreName)) continue;

                var genre = await _db.Genres.FirstOrDefaultAsync(g => g.Name == genreName, ct)
                    ?? _db.ChangeTracker.Entries<Genre>().Select(e => e.Entity).FirstOrDefault(g => g.Name == genreName);

                if (genre is null)
                {
                    genre = new Genre { Name = genreName };
                    _db.Genres.Add(genre);
                }

                var alreadyLinked = title.Genres.Any(tg => tg.Genre != null && tg.Genre.Name == genreName);
                if (!alreadyLinked)
                {
                    title.Genres.Add(new TitleGenre { Title = title, Genre = genre });
                }
            }
        }

        /// <summary>
        /// Mantém apenas as fontes cujo nome bate com a lista Watchmode:AllowedProviders
        /// configurada. Se a lista estiver vazia, não filtra nada (aceita qualquer streaming).
        /// </summary>
        private List<WatchmodeSourceDto> FilterAllowedSources(List<WatchmodeSourceDto> sources)
        {
            if (_watchmodeOptions.AllowedProviders.Count == 0) return sources;

            return sources
                .Where(s => _watchmodeOptions.AllowedProviders.Any(allowed =>
                    string.Equals(allowed, s.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private async Task UpsertProvidersAsync(Title title, List<WatchmodeSourceDto> sources, CancellationToken ct)
        {
            foreach (var source in sources)
            {
                var provider = await _db.Providers.FirstOrDefaultAsync(p => p.WatchmodeSourceId == source.SourceId, ct)
                    ?? _db.ChangeTracker.Entries<Provider>().Select(e => e.Entity)
                        .FirstOrDefault(p => p.WatchmodeSourceId == source.SourceId);

                if (provider is null)
                {
                    provider = new Provider { Name = source.Name, WatchmodeSourceId = source.SourceId };
                    _db.Providers.Add(provider);
                }

                var titleProvider = title.Id != 0
                    ? await _db.TitleProviders.FirstOrDefaultAsync(tp =>
                        tp.TitleId == title.Id &&
                        tp.Provider.WatchmodeSourceId == source.SourceId &&
                        tp.Country == source.Region &&
                        tp.Type == source.Type, ct)
                    : null;

                if (titleProvider is null)
                {
                    titleProvider = new TitleProvider
                    {
                        Title = title,
                        Provider = provider,
                        Country = source.Region,
                        Type = source.Type
                    };
                    _db.TitleProviders.Add(titleProvider);
                }

                titleProvider.WatchUrl = source.WebUrl;
                titleProvider.Price = source.Price;
                titleProvider.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}