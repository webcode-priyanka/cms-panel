using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class SeoSetting
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select content type")]
        [StringLength(50)]
        public string ContentType { get; set; } = "Page";

        [Required(ErrorMessage = "Please enter content ID")]
        public int ContentId { get; set; }

        [Required(ErrorMessage = "Please enter SEO title")]
        [StringLength(200)]
        public string SeoTitle { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        [StringLength(500)]
        public string? MetaKeywords { get; set; }

        [StringLength(500)]
        public string? CanonicalUrl { get; set; }

        [StringLength(100)]
        public string? Robots { get; set; } = "index, follow";

        [StringLength(200)]
        public string? OgTitle { get; set; }

        [StringLength(500)]
        public string? OgDescription { get; set; }

        [StringLength(500)]
        public string? OgImage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
