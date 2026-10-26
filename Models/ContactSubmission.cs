using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class ContactSubmission
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter name")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Please enter message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "New";

        public string? Remarks { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
