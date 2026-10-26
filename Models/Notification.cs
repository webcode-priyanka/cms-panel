using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter notification title")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter notification message")]
        public string Message { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select notification type")]
        [StringLength(50)]
        public string Type { get; set; } = "Info";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Unread";

        public string? Link { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
