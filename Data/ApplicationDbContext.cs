using CMSPanel.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMSPanel.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
     
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<BlogCategory> BlogCategories { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Gallery> Galleries { get; set; }

        public DbSet<Media> Media { get; set; }
        public DbSet<BlogTag> BlogTags { get; set; }
        public DbSet<ProjectCategory> ProjectCategories { get; set; }
        public DbSet<Service> Services { get; set; }

        public DbSet<Product> Products { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<Download> Downloads { get; set; }
        public DbSet<Career> Careers { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<ContactSubmission> ContactSubmissions { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<SeoSetting> SeoSettings { get; set; }
        public DbSet<Redirect> Redirects { get; set; }
        public DbSet<Form> Forms { get; set; }
        public DbSet<FormSubmission> FormSubmissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<CareerApplication> CareerApplications { get; set; }
        public DbSet<TrashItem> TrashItems { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);
        }

    }
    
}
