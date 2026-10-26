using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Career
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter job title")]
        [StringLength(200)]
        public string JobTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter department")]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter location")]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter job type")]
        [StringLength(50)]
        public string JobType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter job description")]
        public string Description { get; set; } = string.Empty;

        public string? Requirements { get; set; }

        public string? Responsibilities { get; set; }

        [StringLength(100)]
        public string? Experience { get; set; }

        [StringLength(100)]
        public string? Salary { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
