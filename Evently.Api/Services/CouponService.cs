using Evently.Api.Data;
using Evently.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api.Services
{
    public class CouponService
    {
        private readonly AppDbContext _context;

        public CouponService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(Coupon? Coupon, decimal Discount, string? Error)> EvaluateAsync(
            Guid eventId, string? code, decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (null, 0, null);

            var normalized = code.Trim().ToUpperInvariant();

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.EventId == eventId && c.Code == normalized);

            if (coupon == null)
                return (null, 0, "Cupom inválido.");

            if (!coupon.IsActive)
                return (null, 0, "Cupom inativo.");

            if (coupon.ExpiresAt.HasValue && DateTime.UtcNow > coupon.ExpiresAt.Value)
                return (null, 0, "Cupom expirado.");

            if (coupon.MaxUses > 0 && coupon.UsedCount >= coupon.MaxUses)
                return (null, 0, "Cupom esgotado.");

            decimal discount = coupon.DiscountType == "fixed"
                ? coupon.DiscountValue
                : Math.Round(subtotal * (coupon.DiscountValue / 100m), 2);

            if (discount > subtotal)
                discount = subtotal;

            if (discount < 0)
                discount = 0;

            return (coupon, discount, null);
        }
    }
}
