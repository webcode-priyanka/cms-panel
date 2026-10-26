using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Media
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FileType { get; set; }

        public long FileSize { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(250)]
        public string? AltText { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}