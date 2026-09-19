using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Mappings;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/history")]
    public class HistoryController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HistoryController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Registra que o usuário logado clicou em "Assistir" nesse título. Se já existir um
        /// registro pra esse título, só atualiza a data (upsert) — não duplica.
        /// </summary>
        [HttpPost("{titleId:int}")]
        public async Task<IActionResult> MarkWatched(int titleId, CancellationToken ct)
        {
            var titleExists = await _db.Titles.AnyAsync(t => t.Id == titleId, ct);
            if (!titleExists) return NotFound();

            var userId = GetUserId();

            var entry = await _db.WatchHistories
                .FirstOrDefaultAsync(w => w.UserId == userId && w.TitleId == titleId, ct);

            if (entry is null)
            {
                _db.WatchHistories.Add(new WatchHistory
                {
                    UserId = userId,
                    TitleId = titleId,
                    LastAccessedAt = DateTime.UtcNow
                });
            }
            else
            {
                entry.LastAccessedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);

            return Ok();
        }

        /// <summary>Lista os títulos assistidos recentemente pelo usuário logado, mais recente primeiro.</summary>
        [HttpGet]
        public async Task<IActionResult> GetHistory([FromQuery] int take = 20, CancellationToken ct = default)
        {
            var userId = GetUserId();

            var titles = await _db.WatchHistories
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.LastAccessedAt)
                .Take(Math.Clamp(take, 1, 50))
                .Include(w => w.Title).ThenInclude(t => t.Genres).ThenInclude(tg => tg.Genre)
                .Include(w => w.Title).ThenInclude(t => t.Providers).ThenInclude(tp => tp.Provider)
                .Select(w => w.Title)
                .ToListAsync(ct);

            return Ok(titles.Select(TitleMapper.ToDto));
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}