using System.ComponentModel.DataAnnotations;

namespace Evently.Api.DTOs
{
    public class CreateEventDto
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data do evento é obrigatória.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "O local é obrigatório.")]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(0, 1000000, ErrorMessage = "O preço deve ser um valor válido.")]
        public decimal Price { get; set; }

        [Range(1, 100000, ErrorMessage = "O total de ingressos deve ser pelo menos 1.")]
        public int TotalTickets { get; set; }

        [MaxLength(60)]
        public string Category { get; set; } = "Outros";

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsFeatured { get; set; } = false;

        [MaxLength(20)]
        public string Status { get; set; } = "Published";
    }
}
