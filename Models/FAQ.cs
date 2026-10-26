using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class FAQ
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Answer { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
