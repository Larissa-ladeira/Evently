using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.Data.Mongo;
using Evently.Api.Models;
using Evently.Api.Models.Mongo;
using Evently.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Evently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckInController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EventlyMongoContext _mongoContext;
        private readonly QrCodeService _qrCodeService;

        public CheckInController(AppDbContext context, EventlyMongoContext mongoContext, QrCodeService qrCodeService)
        {
            _context = context;
            _mongoContext = mongoContext;
            _qrCodeService = qrCodeService;
        }

        [Authorize(Roles = "Organizer,Admin,Staff")]
        [HttpPost("scan")]
        public async Task<IActionResult> Scan([FromBody] CheckInScanDto dto)
        {
            Models.Ticket? ticket = null;

            // Verifica a assinatura do QR (anti-fraude)
            if (_qrCodeService.TryVerifyTicketCode(dto.QrCode, out var ticketId))
            {
                ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            }
            else
            {
                // Compatibilidade com códigos não assinados
                ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.QrCode == dto.QrCode);
            }

            if (ticket == null)
                return NotFound(new { message = "Ingresso não encontrado ou código inválido." });

            if (ticket.Status != "Valid")
                return BadRequest(new { message = $"Ingresso já utilizado ou inválido. Status: {ticket.Status}" });

            ticket.Status = "Used";
            ticket.UsedAt = DateTime.UtcNow;

            var log = new CheckInLog
            {
                EventId = ticket.EventId,
                TicketId = ticket.Id,
                QrCode = ticket.QrCode,
                CheckedInBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!),
                DeviceInfo = dto.DeviceInfo
            };

            await _mongoContext.CheckInLogs.InsertOneAsync(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Check-in realizado com sucesso!", ticketId = ticket.Id });
        }

        [Authorize(Roles = "Organizer,Admin,Staff")]
        [HttpGet("logs/{eventId}")]
        public async Task<IActionResult> GetLogs(Guid eventId)
        {
            var logs = await _mongoContext.CheckInLogs
                .Find(l => l.EventId == eventId)
                .ToListAsync();

            return Ok(logs);
        }
    }

    public class CheckInScanDto
    {
        public string QrCode { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
    }
}
