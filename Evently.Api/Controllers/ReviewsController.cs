using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.DTOs;
using Evently.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(Guid eventId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.EventId == eventId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    UserName = r.User!.Name
                })
                .ToListAsync();

            var average = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            return Ok(new { average, count = reviews.Count, reviews });
        }

        [Authorize]
        [HttpPost("event/{eventId}")]
        public async Task<IActionResult> Create(Guid eventId, [FromBody] CreateReviewDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Evento não encontrado." });

            var hasTicket = await _context.Tickets.AnyAsync(t => t.EventId == eventId && t.UserId == userId);
            if (!hasTicket)
                return BadRequest(new { message = "Só podes avaliar eventos em que compraste ingresso." });

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { message = "A classificação deve ser entre 1 e 5." });

            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (existing != null)
            {
                existing.Rating = dto.Rating;
                existing.Comment = dto.Comment;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Reviews.Add(new Review
                {
                    EventId = eventId,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = dto.Comment
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Avaliação registada. Obrigado!" });
        }
    }
}
