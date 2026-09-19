using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/providers")]
    public class ProvidersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProvidersController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>Lista todos os streamings que o catálogo cobre (os mesmos de Watchmode:AllowedProviders).</summary>
        [HttpGet]
        public async Task<IActionResult> GetProviders(CancellationToken ct)
        {
            var providers = await _db.Providers
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.LogoUrl })
                .ToListAsync(ct);

            return Ok(providers);
        }

        /// <summary>IDs dos streamings que o usuário logado marcou como "eu assino".</summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProviders(CancellationToken ct)
        {
            var providerIds = await _db.UserProviders
                .Where(up => up.UserId == GetUserId())
                .Select(up => up.ProviderId)
                .ToListAsync(ct);

            return Ok(providerIds);
        }

        /// <summary>Substitui a lista de streamings assinados pelo usuário logado.</summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProviders(UpdateUserProvidersRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var existing = await _db.UserProviders.Where(up => up.UserId == userId).ToListAsync(ct);
            _db.UserProviders.RemoveRange(existing);

            var distinctIds = (request.ProviderIds ?? []).Distinct().ToList();
            foreach (var providerId in distinctIds)
            {
                _db.UserProviders.Add(new UserProvider { UserId = userId, ProviderId = providerId });
            }

            await _db.SaveChangesAsync(ct);

            return Ok(distinctIds);
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public class UpdateUserProvidersRequest
    {
        public List<int> ProviderIds { get; set; } = [];
    }
}