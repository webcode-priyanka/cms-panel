using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Redirect
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter source URL")]
        [StringLength(500)]
        public string SourceUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter destination URL")]
        [StringLength(500)]
        public string DestinationUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select redirect type")]
        [StringLength(10)]
        public string RedirectType { get; set; } = "301";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
