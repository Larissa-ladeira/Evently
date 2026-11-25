using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.DTOs;
using Evently.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly QrCodeService _qrCodeService;

        public TicketsController(AppDbContext context, QrCodeService qrCodeService)
        {
            _context = context;
            _qrCodeService = qrCodeService;
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyTickets()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var tickets = await _context.Tickets
                .Where(t => t.UserId == userId)
                .Include(t => t.Event)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var result = tickets.Select(t => new
            {
                t.Id,
                t.OrderId,
                t.TicketType,
                t.Status,
                t.UsedAt,
                Event = t.Event == null ? null : new
                {
                    t.Event.Id,
                    t.Event.Title,
                    t.Event.Date,
                    t.Event.Location,
                    t.Event.ImageUrl,
                    t.Event.Category
                },
                QrCode = _qrCodeService.GenerateQrCodeBase64(t.QrCode),
                QrCodeText = t.QrCode
            });

            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}/qrcode")]
        public async Task<IActionResult> GetQrCode(Guid id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (ticket.UserId != userId && role != "Admin" && role != "Organizer" && role != "Staff")
                return Forbid();

            var qrBytes = _qrCodeService.GenerateQrCode(ticket.QrCode);
            return File(qrBytes, "image/png");
        }

        [Authorize]
        [HttpPost("{id}/transfer")]
        public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferTicketDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound(new { message = "Ingresso não encontrado." });

            if (ticket.UserId != userId)
                return Forbid();

            if (ticket.Status != "Valid")
                return BadRequest(new { message = "Apenas ingressos válidos podem ser transferidos." });

            var target = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());

            if (target == null)
                return NotFound(new { message = "Não existe nenhum utilizador com esse e-mail. Peça-lhe para criar conta primeiro." });

            if (target.Id == userId)
                return BadRequest(new { message = "Não podes transferir um ingresso para ti próprio." });

            ticket.UserId = target.Id;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Ingresso transferido para {target.Name}.", recipient = target.Name });
        }
    }
}
