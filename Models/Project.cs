using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [StringLength(150)]
        public string? ClientName { get; set; }

        [StringLength(150)]
        public string? ProjectType { get; set; }


        public int? ProjectCategoryId { get; set; }

        public ProjectCategory? ProjectCategory { get; set; }

        [StringLength(500)]
        public string? FeaturedImage { get; set; }

        [StringLength(500)]
        public string? Technologies { get; set; }

        public string? ProjectUrl { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        public bool IsFeatured { get; set; } = false;

        public DateTime? ProjectDate { get; set; }

        [StringLength(250)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
