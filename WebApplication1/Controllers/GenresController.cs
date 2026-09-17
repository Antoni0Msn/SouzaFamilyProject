using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/genres")]
    public class GenresController : ControllerBase
    {
        private readonly AppDbContext _db;

        public GenresController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>Lista os gêneros que têm pelo menos um título no catálogo, em ordem alfabética.</summary>
        [HttpGet]
        public async Task<IActionResult> GetGenres(CancellationToken ct)
        {
            var genres = await _db.Genres
                .Where(g => g.Titles.Any())
                .OrderBy(g => g.Name)
                .Select(g => new { g.Id, g.Name })
                .ToListAsync(ct);

            return Ok(genres);
        }
    }
}
