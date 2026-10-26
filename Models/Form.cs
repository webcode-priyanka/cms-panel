using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Form
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter form name")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter form slug")]
        [StringLength(200)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
