using System;
using System.ComponentModel.DataAnnotations;

namespace CMSPanel.Models
{
    public class Setting
    {
        public int Id { get; set; }

        // Site Identity
        [StringLength(200)]
        public string? SiteName { get; set; }

        [StringLength(500)]
        public string? SiteTagline { get; set; }

        [StringLength(500)]
        public string? Logo { get; set; }

        [StringLength(500)]
        public string? Favicon { get; set; }

        // Contact Information
        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        // Business Hours
        [StringLength(200)]
        public string? BusinessHours { get; set; }

        // Social Links
        [StringLength(500)]
        public string? FacebookUrl { get; set; }

        [StringLength(500)]
        public string? InstagramUrl { get; set; }

        [StringLength(500)]
        public string? TwitterUrl { get; set; }

        [StringLength(500)]
        public string? LinkedInUrl { get; set; }

        [StringLength(500)]
        public string? YouTubeUrl { get; set; }

        // Branding
        [StringLength(20)]
        public string? PrimaryColor { get; set; }

        [StringLength(20)]
        public string? SecondaryColor { get; set; }

        // Operational Settings
        public bool MaintenanceMode { get; set; } = false;

        public bool EnableNotifications { get; set; } = true;

        [StringLength(100)]
        public string? TimeZone { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
