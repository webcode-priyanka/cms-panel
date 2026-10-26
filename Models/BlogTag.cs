using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class BlogTag
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Slug { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // =========================
        // BLOG RELATION
        // =========================

        public virtual ICollection<Blog> Blogs { get; set; }
            = new List<Blog>();
    }
}