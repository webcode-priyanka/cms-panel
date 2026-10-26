using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Page
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        public string? Content { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        public bool IsPublished { get; set; } = false;

        [StringLength(250)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        public bool IsVisible { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
