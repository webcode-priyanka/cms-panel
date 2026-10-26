using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter menu title")]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select menu location")]
        [StringLength(50)]
        public string Location { get; set; } = "Header";

        [Required(ErrorMessage = "Please enter URL")]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int? ParentId { get; set; }

        public MenuItem? Parent { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsVisible { get; set; } = true;

        [StringLength(20)]
        public string? Target { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
