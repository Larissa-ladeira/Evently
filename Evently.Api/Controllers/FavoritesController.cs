using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var events = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Event).ThenInclude(e => e!.Organizer)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    FavoriteId = f.Id,
                    f.Event!.Id,
                    f.Event.Title,
                    f.Event.Description,
                    f.Event.Date,
                    f.Event.Location,
                    f.Event.Price,
                    f.Event.Category,
                    f.Event.ImageUrl,
                    OrganizerName = f.Event.Organizer!.Name
                })
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("ids")]
        public async Task<IActionResult> GetMyFavoriteIds()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var ids = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.EventId)
                .ToListAsync();
            return Ok(ids);
        }

        [HttpPost("toggle/{eventId}")]
        public async Task<IActionResult> Toggle(Guid eventId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Evento não encontrado." });

            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.EventId == eventId);

            if (existing != null)
            {
                _context.Favorites.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { favorited = false });
            }

            _context.Favorites.Add(new Favorite { UserId = userId, EventId = eventId });
            await _context.SaveChangesAsync();
            return Ok(new { favorited = true });
        }
    }
}
