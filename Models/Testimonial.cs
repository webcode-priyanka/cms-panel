using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Testimonial
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Client name is required")]
        [StringLength(150)]
        public string ClientName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Designation { get; set; }

        [StringLength(150)]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Testimonial content is required")]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; } = 5;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}