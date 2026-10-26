using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class TrashItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Module { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ItemName { get; set; }

        [Required]
        public string ItemId { get; set; } = string.Empty;

        public string? Data { get; set; }

        [StringLength(150)]
        public string? DeletedBy { get; set; }

        public DateTime DeletedAt { get; set; } = DateTime.Now;

        public bool IsRestored { get; set; } = false;

        public DateTime? RestoredAt { get; set; }

        [StringLength(150)]
        public string? RestoredBy { get; set; }
    }
}
