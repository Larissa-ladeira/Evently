using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.Data.Mongo;
using Evently.Api.Models.Mongo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Evently.Api.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EventlyMongoContext _mongoContext;

        public ReportsController(AppDbContext context, EventlyMongoContext mongoContext)
        {
            _context = context;
            _mongoContext = mongoContext;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var (eventIds, _) = await GetOrganizerEventIdsAsync();

            var paidOrders = await _context.Orders
                .Where(o => eventIds.Contains(o.EventId) && o.Status == "Paid")
                .ToListAsync();

            var tickets = await _context.Tickets
                .Where(t => eventIds.Contains(t.EventId))
                .ToListAsync();

            var checkedIn = tickets.Count(t => t.Status == "Used");

            return Ok(new
            {
                totalEvents = eventIds.Count,
                paidOrders = paidOrders.Count,
                totalRevenue = paidOrders.Sum(o => o.TotalAmount),
                ticketsSold = tickets.Count,
                ticketsCheckedIn = checkedIn
            });
        }

        [HttpGet("events")]
        public async Task<IActionResult> EventsReport()
        {
            var (eventIds, _) = await GetOrganizerEventIdsAsync();

            var events = await _context.Events
                .Where(e => eventIds.Contains(e.Id))
                .Select(e => new { e.Id, e.Title, e.Date })
                .ToListAsync();

            var paidOrders = await _context.Orders
                .Where(o => eventIds.Contains(o.EventId) && o.Status == "Paid")
                .ToListAsync();

            var tickets = await _context.Tickets
                .Where(t => eventIds.Contains(t.EventId))
                .ToListAsync();

            var report = events.Select(e =>
            {
                var evTickets = tickets.Where(t => t.EventId == e.Id).ToList();
                var evOrders = paidOrders.Where(o => o.EventId == e.Id).ToList();
                return new
                {
                    e.Id,
                    e.Title,
                    e.Date,
                    paidOrders = evOrders.Count,
                    ticketsSold = evTickets.Count,
                    ticketsCheckedIn = evTickets.Count(t => t.Status == "Used"),
                    revenue = evOrders.Sum(o => o.TotalAmount)
                };
            }).OrderByDescending(r => r.Date).ToList();

            return Ok(report);
        }

        [HttpGet("events/{eventId}")]
        public async Task<IActionResult> EventReport(Guid eventId)
        {
            var (_, role) = await GetOrganizerEventIdsAsync();
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Evento não encontrado." });

            if (ev.OrganizerId != userId && role != "Admin")
                return Forbid();

            var paidOrders = await _context.Orders
                .Where(o => o.EventId == eventId && o.Status == "Paid")
                .ToListAsync();

            var batches = await _context.TicketBatches
                .Where(b => b.EventId == eventId)
                .Select(b => new { b.Id, b.Name, b.Price, b.Quantity, b.Sold })
                .ToListAsync();

            var tickets = await _context.Tickets
                .Where(t => t.EventId == eventId)
                .ToListAsync();

            var checkIns = await _mongoContext.CheckInLogs
                .CountDocumentsAsync(Builders<CheckInLog>.Filter.Eq(l => l.EventId, eventId));

            return Ok(new
            {
                eventId,
                ev.Title,
                paidOrders = paidOrders.Count,
                revenue = paidOrders.Sum(o => o.TotalAmount),
                ticketsSold = tickets.Count,
                ticketsCheckedIn = tickets.Count(t => t.Status == "Used"),
                checkInLogs = checkIns,
                batches
            });
        }

        [HttpGet("recent-orders")]
        public async Task<IActionResult> RecentOrders([FromQuery] int take = 10)
        {
            var (eventIds, _) = await GetOrganizerEventIdsAsync();

            var orders = await _context.Orders
                .Where(o => eventIds.Contains(o.EventId))
                .Include(o => o.Event)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(Math.Clamp(take, 1, 50))
                .ToListAsync();

            var result = orders.Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                o.CouponCode,
                o.DiscountAmount,
                EventTitle = o.Event?.Title,
                BuyerName = o.User?.Name,
                BuyerEmail = o.User?.Email
            });

            return Ok(result);
        }

        private async Task<(List<Guid> EventIds, string Role)> GetOrganizerEventIdsAsync()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var query = _context.Events.AsQueryable();
            if (role != "Admin")
                query = query.Where(e => e.OrganizerId == userId);

            var ids = await query.Select(e => e.Id).ToListAsync();
            return (ids, role);
        }
    }
}
