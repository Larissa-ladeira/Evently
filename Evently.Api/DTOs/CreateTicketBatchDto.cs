using System.ComponentModel.DataAnnotations;

namespace Evently.Api.DTOs
{
    public class CreateTicketBatchDto
    {
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

        public DateTime? SaleStart { get; set; }
        public DateTime? SaleEnd { get; set; }
    }
}
