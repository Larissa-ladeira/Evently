using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Evently.Api.Models
{
    public class Event
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int TotalTickets { get; set; }

        [MaxLength(60)]
        public string Category { get; set; } = "Outros";

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // Draft, Published, Cancelled
        [MaxLength(20)]
        public string Status { get; set; } = "Published";

        public bool IsFeatured { get; set; } = false;

        // Relacionamento com o Organizador (User)
        [Required]
        public Guid OrganizerId { get; set; }
        
        [ForeignKey("OrganizerId")]
        public User? Organizer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}