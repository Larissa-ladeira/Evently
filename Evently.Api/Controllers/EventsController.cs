using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.DTOs;
using Evently.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? q,
            [FromQuery] string? category,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sort,
            [FromQuery] bool? featured)
        {
            var query = _context.Events.Include(e => e.Organizer).AsQueryable();

            query = query.Where(e => e.Status == "Published");

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(e =>
                    EF.Functions.ILike(e.Title, $"%{term}%") ||
                    EF.Functions.ILike(e.Description, $"%{term}%") ||
                    EF.Functions.ILike(e.Location, $"%{term}%"));
            }

            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "Todos", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.Category == category);

            if (from.HasValue)
                query = query.Where(e => e.Date >= ToUtc(from.Value));

            if (to.HasValue)
                query = query.Where(e => e.Date <= ToUtc(to.Value));

            if (minPrice.HasValue)
                query = query.Where(e => e.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(e => e.Price <= maxPrice.Value);

            if (featured == true)
                query = query.Where(e => e.IsFeatured);

            query = sort switch
            {
                "price_asc" => query.OrderBy(e => e.Price),
                "price_desc" => query.OrderByDescending(e => e.Price),
                "date_desc" => query.OrderByDescending(e => e.Date),
                _ => query.OrderBy(e => e.Date)
            };

            var events = await query.ToListAsync();
            var ids = events.Select(e => e.Id).ToList();

            var ratings = await _context.Reviews
                .Where(r => ids.Contains(r.EventId))
                .GroupBy(r => r.EventId)
                .Select(g => new { EventId = g.Key, Avg = g.Average(x => (double)x.Rating), Count = g.Count() })
                .ToDictionaryAsync(x => x.EventId);

            var stock = await _context.TicketBatches
                .Where(b => ids.Contains(b.EventId))
                .GroupBy(b => b.EventId)
                .Select(g => new { EventId = g.Key, Sold = g.Sum(b => b.Sold), Capacity = g.Sum(b => b.Quantity) })
                .ToDictionaryAsync(x => x.EventId);

            var result = events.Select(e =>
            {
                ratings.TryGetValue(e.Id, out var r);
                stock.TryGetValue(e.Id, out var s);
                var capacity = s?.Capacity ?? e.TotalTickets;
                var sold = s?.Sold ?? 0;
                return new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Location,
                    e.Price,
                    e.TotalTickets,
                    e.Category,
                    e.ImageUrl,
                    e.IsFeatured,
                    OrganizerName = e.Organizer?.Name,
                    Rating = r == null ? (double?)null : Math.Round(r.Avg, 1),
                    ReviewCount = r?.Count ?? 0,
                    Sold = sold,
                    Available = capacity - sold,
                    SoldOut = capacity > 0 && sold >= capacity
                };
            });

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var ev = await _context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            var ratingAgg = await _context.Reviews
                .Where(r => r.EventId == id)
                .GroupBy(r => r.EventId)
                .Select(g => new { Avg = g.Average(x => (double)x.Rating), Count = g.Count() })
                .FirstOrDefaultAsync();

            var batches = await _context.TicketBatches
                .Where(b => b.EventId == id)
                .Select(b => new { b.Quantity, b.Sold })
                .ToListAsync();

            var capacity = batches.Count > 0 ? batches.Sum(b => b.Quantity) : ev.TotalTickets;
            var sold = batches.Sum(b => b.Sold);

            return Ok(new
            {
                ev.Id,
                ev.Title,
                ev.Description,
                ev.Date,
                ev.Location,
                ev.Price,
                ev.TotalTickets,
                ev.Category,
                ev.ImageUrl,
                ev.IsFeatured,
                ev.Status,
                OrganizerId = ev.OrganizerId,
                OrganizerName = ev.Organizer?.Name,
                Rating = ratingAgg == null ? (double?)null : Math.Round(ratingAgg.Avg, 1),
                ReviewCount = ratingAgg?.Count ?? 0,
                Sold = sold,
                Available = capacity - sold,
                SoldOut = capacity > 0 && sold >= capacity
            });
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.Events.Include(e => e.Organizer).AsQueryable();
            if (role != "Admin")
                query = query.Where(e => e.OrganizerId == userId);

            var events = await query.OrderByDescending(e => e.Date).ToListAsync();
            var ids = events.Select(e => e.Id).ToList();

            var stock = await _context.TicketBatches
                .Where(b => ids.Contains(b.EventId))
                .GroupBy(b => b.EventId)
                .Select(g => new { EventId = g.Key, Sold = g.Sum(b => b.Sold), Capacity = g.Sum(b => b.Quantity) })
                .ToDictionaryAsync(x => x.EventId);

            var result = events.Select(e =>
            {
                stock.TryGetValue(e.Id, out var s);
                var capacity = s?.Capacity ?? e.TotalTickets;
                var sold = s?.Sold ?? 0;
                return new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.Date,
                    e.Location,
                    e.Price,
                    e.TotalTickets,
                    e.Category,
                    e.ImageUrl,
                    e.IsFeatured,
                    e.Status,
                    OrganizerName = e.Organizer?.Name,
                    Sold = sold,
                    Capacity = capacity,
                    Available = capacity - sold,
                    SoldOut = capacity > 0 && sold >= capacity
                };
            });

            return Ok(result);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var organizerId))
            {
                return Unauthorized(new { message = "Usuário não autenticado." });
            }

            var newEvent = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                Date = ToUtc(dto.Date),
                Location = dto.Location,
                Price = dto.Price,
                TotalTickets = dto.TotalTickets,
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "Outros" : dto.Category,
                ImageUrl = dto.ImageUrl,
                IsFeatured = dto.IsFeatured,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Published" : dto.Status,
                OrganizerId = organizerId
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Evento criado com sucesso!", eventId = newEvent.Id });
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateEventDto dto)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (ev.OrganizerId != userId && role != "Admin")
                return Forbid();

            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.Date = ToUtc(dto.Date);
            ev.Location = dto.Location;
            ev.Price = dto.Price;
            ev.TotalTickets = dto.TotalTickets;
            ev.Category = string.IsNullOrWhiteSpace(dto.Category) ? "Outros" : dto.Category;
            ev.ImageUrl = dto.ImageUrl;
            ev.IsFeatured = dto.IsFeatured;
            ev.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Published" : dto.Status;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Evento atualizado com sucesso." });
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (ev.OrganizerId != userId && role != "Admin")
                return Forbid();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
