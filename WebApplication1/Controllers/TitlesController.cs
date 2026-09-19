using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs.Common;
using WebApplication1.DTOs.Titles;
using WebApplication1.Mappings;
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
        /// GET /api/titles?type=Movie&amp;genre=Drama&amp;q=busca&amp;page=1&amp;pageSize=20
        /// type: "Movie" ou "Series" (omitido = os dois). genre: nome exato do gênero
        /// (vem de GET /api/genres). q: busca por nome. page/pageSize: paginação (pageSize máx 100).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTitles(
            [FromQuery] string? type,
            [FromQuery] string? genre,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = BaseQuery();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(t => t.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                query = query.Where(t => t.Genres.Any(tg => tg.Genre.Name == genre));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(t => EF.Functions.ILike(t.Name, $"%{q}%"));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(t => t.Rating)
                .ThenByDescending(t => t.ReleaseDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var result = new PagedResultDto<TitleResponseDto>
            {
                Items = items.Select(TitleMapper.ToDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTitleById(int id, CancellationToken ct)
        {
            var title = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (title is null) return NotFound();

            return Ok(TitleMapper.ToDto(title));
        }

        private IQueryable<Title> BaseQuery()
        {
            return _db.Titles
                .Include(t => t.Genres).ThenInclude(tg => tg.Genre)
                .Include(t => t.Providers).ThenInclude(tp => tp.Provider)
                .AsNoTracking();
        }
    }
}