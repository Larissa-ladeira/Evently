using System.ComponentModel.DataAnnotations;

namespace Evently.Api.Models
{
    public class TicketBatch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid EventId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 1000000)]
        public int Quantity { get; set; }

        [Required]
        [Range(0, 1000000)]
        public int Sold { get; set; } = 0;

        public DateTime? SaleStart { get; set; }
        public DateTime? SaleEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
