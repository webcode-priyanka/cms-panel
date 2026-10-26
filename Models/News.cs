using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class News
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter news title")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter news slug")]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter news content")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter news excerpt")]
        public string Excerpt { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter author name")]
        [StringLength(100)]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select publication date")]
        [DataType(DataType.Date)]
        public DateTime PublicationDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please enter image path")]
        public string Image { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter attachment path")]
        public string Attachment { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter category")]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select status")]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        [Required(ErrorMessage = "Please enter meta title")]
        [StringLength(200)]
        public string MetaTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter meta description")]
        [StringLength(500)]
        public string MetaDescription { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
