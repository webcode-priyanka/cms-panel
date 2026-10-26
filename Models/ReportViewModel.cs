using System;

namespace CMSPanel.Models
{
    public class ReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalEditors { get; set; }

        public int TotalPages { get; set; }
        public int TotalBlogs { get; set; }
        public int TotalProjects { get; set; }

        public int TotalNotifications { get; set; }
        public int TotalFormSubmissions { get; set; }
        public int TotalContactSubmissions { get; set; }

        public int TotalCareerApplications { get; set; }

        public int TotalAuditLogs { get; set; }

        public int PublishedBlogs { get; set; }
        public int DraftBlogs { get; set; }

        public int PublishedPages { get; set; }
        public int DraftPages { get; set; }

        public int RecentActivities { get; set; }
    }
}