using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace CMSPanel.Models
{
    public class JobApplication
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter applicant name")]
        [StringLength(150)]
        public string ApplicantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter phone number")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Please enter a valid 10-digit phone number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a job")]
        public int CareerId { get; set; }

        public Career? Career { get; set; }

        // Saved file path in database
        public string? ResumeFile { get; set; }

        // Actual uploaded file
        [NotMapped]
        public IFormFile? ResumeUpload { get; set; }

        public string? CoverLetter { get; set; }

        public string? Skills { get; set; }

        public string? Experience { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public string? Remarks { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}