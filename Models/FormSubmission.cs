using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class FormSubmission
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select form")]
        public int FormId { get; set; }

        public Form? Form { get; set; }

        [Required(ErrorMessage = "Please enter submitter name")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        public string? SubmissionData { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "New";

        public string? Remarks { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
