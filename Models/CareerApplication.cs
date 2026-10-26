using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class CareerApplication
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter applicant name")]
        [StringLength(150)]
        public string ApplicantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? CareerTitle { get; set; }

        public int? CareerId { get; set; }

        [StringLength(500)]
        public string? ResumePath { get; set; }

        [StringLength(100)]
        public string Status { get; set; } = "Pending";

        public string? CoverLetter { get; set; }

        public string? Notes { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
