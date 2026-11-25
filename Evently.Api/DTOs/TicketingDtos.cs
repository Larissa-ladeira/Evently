using System.ComponentModel.DataAnnotations;

namespace Evently.Api.DTOs
{
    public class CreateCouponDto
    {
        [Required]
        public Guid EventId { get; set; }

        [Required]
        [MaxLength(40)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string DiscountType { get; set; } = "percent"; // percent | fixed

        [Range(0.01, 1000000)]
        public decimal DiscountValue { get; set; }

        public int MaxUses { get; set; } = 0;

        public DateTime? ExpiresAt { get; set; }
    }

    public class ApplyCouponDto
    {
        [Required]
        public Guid EventId { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        [Range(0, 1000000)]
        public decimal Subtotal { get; set; }
    }

    public class TransferTicketDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class CreateReviewDto
    {
        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
