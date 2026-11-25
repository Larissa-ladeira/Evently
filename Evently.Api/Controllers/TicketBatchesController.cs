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
    public class TicketBatchesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketBatchesController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTicketBatchDto dto)
        {
            var ev = await _context.Events.FindAsync(dto.EventId);
            if (ev == null) return NotFound(new { message = "Evento não encontrado." });
            if (!await CanManageAsync(ev)) return Forbid();

            var batch = new TicketBatch
            {
                EventId = dto.EventId,
                Name = dto.Name,
                Price = dto.Price,
                Quantity = dto.Quantity,
                Sold = 0,
                SaleStart = dto.SaleStart.HasValue ? ToUtc(dto.SaleStart.Value) : null,
                SaleEnd = dto.SaleEnd.HasValue ? ToUtc(dto.SaleEnd.Value) : null
            };

            _context.TicketBatches.Add(batch);
            await _context.SaveChangesAsync();

            return Created("", batch);
        }

        [AllowAnonymous]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(Guid eventId)
        {
            var batches = await _context.TicketBatches
                .Where(b => b.EventId == eventId)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var result = batches.Select(b => new
            {
                b.Id,
                b.EventId,
                b.Name,
                b.Price,
                b.Quantity,
                b.Sold,
                Available = b.Quantity - b.Sold,
                b.SaleStart,
                b.SaleEnd,
                SoldOut = b.Quantity - b.Sold <= 0,
                OnSale = (!b.SaleStart.HasValue || now >= b.SaleStart.Value)
                         && (!b.SaleEnd.HasValue || now <= b.SaleEnd.Value)
            });

            return Ok(result);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateTicketBatchDto dto)
        {
            var batch = await _context.TicketBatches.FindAsync(id);
            if (batch == null) return NotFound();

            var ev = await _context.Events.FindAsync(batch.EventId);
            if (ev == null || !await CanManageAsync(ev)) return Forbid();

            if (dto.Quantity < batch.Sold)
                return BadRequest(new { message = $"A quantidade não pode ser inferior aos {batch.Sold} já vendidos." });

            batch.Name = dto.Name;
            batch.Price = dto.Price;
            batch.Quantity = dto.Quantity;
            batch.SaleStart = dto.SaleStart.HasValue ? ToUtc(dto.SaleStart.Value) : null;
            batch.SaleEnd = dto.SaleEnd.HasValue ? ToUtc(dto.SaleEnd.Value) : null;

            await _context.SaveChangesAsync();
            return Ok(batch);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var batch = await _context.TicketBatches.FindAsync(id);
            if (batch == null) return NotFound();

            var ev = await _context.Events.FindAsync(batch.EventId);
            if (ev == null || !await CanManageAsync(ev)) return Forbid();

            if (batch.Sold > 0)
                return BadRequest(new { message = "Não é possível remover um lote com ingressos vendidos." });

            _context.TicketBatches.Remove(batch);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<bool> CanManageAsync(Event ev)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return await Task.FromResult(ev.OrganizerId == userId || role == "Admin");
        }

        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
