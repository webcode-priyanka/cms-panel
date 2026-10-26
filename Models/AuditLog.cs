using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [StringLength(150)]
        public string? UserId { get; set; }

        [StringLength(150)]
        public string? UserName { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Module { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Resource { get; set; }

        public string? ResourceId { get; set; }

        [StringLength(50)]
        public string? Result { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}