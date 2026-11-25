using System.ComponentModel.DataAnnotations;

namespace Evently.Api.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        public Guid EventId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "O pedido deve conter pelo menos um item.")]
        public List<OrderItemDto> Items { get; set; } = new();

        public Dictionary<string, string>? FormAnswers { get; set; }

        [MaxLength(40)]
        public string? CouponCode { get; set; }
    }

    public class OrderItemDto
    {
        [Required]
        public Guid TicketTypeId { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "A quantidade deve estar entre 1 e 1000.")]
        public int Quantity { get; set; }
    }
}
