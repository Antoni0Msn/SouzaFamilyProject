using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs.Titles;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/titles")]
    public class TitlesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TitlesController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/titles?type=Movie&amp;q=busca&amp;take=20
        /// type: "Movie" ou "Series" (omitido = os dois). q: busca por nome. take: limite (padrão 30, máx 100).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTitles(
            [FromQuery] string? type, [FromQuery] string? q, [FromQuery] int take = 30, CancellationToken ct = default)
        {
            var query = BaseQuery();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(t => t.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(t => EF.Functions.ILike(t.Name, $"%{q}%"));
            }

            var titles = await query
                .OrderByDescending(t => t.Rating)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync(ct);

            return Ok(titles.Select(MapToDto));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTitleById(int id, CancellationToken ct)
        {
            var title = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (title is null) return NotFound();

            return Ok(MapToDto(title));
        }

        private IQueryable<Title> BaseQuery()
        {
            return _db.Titles
                .Include(t => t.Genres).ThenInclude(tg => tg.Genre)
                .Include(t => t.Providers).ThenInclude(tp => tp.Provider)
                .AsNoTracking();
        }

        private static TitleResponseDto MapToDto(Title title) => new()
        {
            Id = title.Id,
            Name = title.Name,
            OriginalName = title.OriginalName,
            Description = title.Description,
            Type = title.Type,
            ReleaseDate = title.ReleaseDate,
            Rating = title.Rating,
            PosterUrl = title.PosterUrl,
            BackdropUrl = title.BackdropUrl,
            Genres = title.Genres
                .Where(tg => tg.Genre != null)
                .Select(tg => tg.Genre.Name)
                .ToList(),
            Providers = title.Providers
                .Where(tp => tp.Provider != null)
                .Select(tp => new ProviderResponseDto
                {
                    Id = tp.ProviderId,
                    Name = tp.Provider.Name,
                    LogoUrl = tp.Provider.LogoUrl,
                    WatchUrl = tp.WatchUrl,
                    Type = tp.Type
                })
                .ToList()
        };
    }
}