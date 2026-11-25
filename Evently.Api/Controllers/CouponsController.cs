using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.DTOs;
using Evently.Api.Models;
using Evently.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CouponService _couponService;

        public CouponsController(AppDbContext context, CouponService couponService)
        {
            _context = context;
            _couponService = couponService;
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCouponDto dto)
        {
            var ev = await _context.Events.FindAsync(dto.EventId);
            if (ev == null) return NotFound(new { message = "Evento não encontrado." });

            if (!await CanManageAsync(ev))
                return Forbid();

            var code = dto.Code.Trim().ToUpperInvariant();

            if (await _context.Coupons.AnyAsync(c => c.EventId == dto.EventId && c.Code == code))
                return Conflict(new { message = "Já existe um cupom com este código para este evento." });

            if (dto.DiscountType == "percent" && dto.DiscountValue > 100)
                return BadRequest(new { message = "Um desconto percentual não pode exceder 100%." });

            var coupon = new Coupon
            {
                EventId = dto.EventId,
                Code = code,
                DiscountType = dto.DiscountType == "fixed" ? "fixed" : "percent",
                DiscountValue = dto.DiscountValue,
                MaxUses = dto.MaxUses,
                ExpiresAt = dto.ExpiresAt.HasValue ? ToUtc(dto.ExpiresAt.Value) : null,
                IsActive = true
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            return StatusCode(201, coupon);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(Guid eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound();
            if (!await CanManageAsync(ev)) return Forbid();

            var coupons = await _context.Coupons
                .Where(c => c.EventId == eventId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(coupons);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            var ev = await _context.Events.FindAsync(coupon.EventId);
            if (ev == null || !await CanManageAsync(ev)) return Forbid();

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] ApplyCouponDto dto)
        {
            var (coupon, discount, error) = await _couponService.EvaluateAsync(dto.EventId, dto.Code, dto.Subtotal);

            if (error != null)
                return BadRequest(new { message = error });

            if (coupon == null)
                return BadRequest(new { message = "Informe um cupom." });

            return Ok(new
            {
                code = coupon.Code,
                discountType = coupon.DiscountType,
                discountValue = coupon.DiscountValue,
                discount,
                message = "Cupom aplicado!"
            });
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
