using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSPanel.Models
{
    public class Blog
    {
        public int Id { get; set; }

        // =========================
        // BASIC BLOG INFORMATION
        // =========================

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Excerpt { get; set; }

        public string? Content { get; set; }

        // =========================
        // IMAGE
        // =========================

        public string? FeaturedImage { get; set; }

        // =========================
        // AUTHOR
        // =========================

        [StringLength(150)]
        public string? Author { get; set; }

        // =========================
        // CATEGORY
        // =========================

        public int? BlogCategoryId { get; set; }

        [ForeignKey("BlogCategoryId")]
        public virtual BlogCategory? BlogCategory { get; set; }

        // =========================
        // TAGS
        // =========================

        [StringLength(500)]
        public string? Tags { get; set; }

        public virtual ICollection<BlogTag> BlogTags { get; set; }
            = new List<BlogTag>();

        // =========================
        // PUBLISHING
        // =========================

        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        public bool IsPublished { get; set; } = false;

        public bool IsFeatured { get; set; } = false;

        public DateTime? PublicationDate { get; set; }

        // =========================
        // SEO
        // =========================

        [StringLength(250)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        // =========================
        // AUDIT / DATES
        // =========================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}