
using Microsoft.AspNetCore.Hosting;
using System;
using CMSPanel.Data;
using CMSPanel.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace CMSPanel.Controllers
{
    public class CMSController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public CMSController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers =
                await _userManager.Users.CountAsync();

            ViewBag.TotalRoles =
                await _roleManager.Roles.CountAsync();

            ViewBag.TotalPages =
                await _context.Pages.CountAsync();

            ViewBag.TotalBlogs =
                await _context.Blogs.CountAsync();

            ViewBag.TotalProjects =
                await _context.Projects.CountAsync();

            ViewBag.TotalCareers =
                await _context.Careers.CountAsync();

            ViewBag.TotalContactSubmissions =
                await _context.ContactSubmissions.CountAsync();

            ViewBag.PendingApprovals =
                await _context.Pages.CountAsync(p => p.Status == "Draft")
                + await _context.Blogs.CountAsync(b => b.Status == "Draft")
                + await _context.Projects.CountAsync(p => p.Status == "Draft");

            ViewBag.UnreadNotifications =
                await _context.Notifications.CountAsync(
                    n => n.Status == "Unread");

            ViewBag.RecentActivities =
                await _context.AuditLogs
                    .AsNoTracking()
                    .OrderByDescending(a => a.Id)
                    .Take(5)
                    .ToListAsync();

            return View();
        }
        // =====================================================
        // USERS MANAGEMENT
        // =====================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Users(string searchString)


        {
            var users = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                users = users.Where(u =>
                    (u.UserName != null && u.UserName.Contains(searchString)) ||
                    (u.Email != null && u.Email.Contains(searchString)));
            }

            ViewBag.SearchString = searchString;

            var userList = await users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            return View(userList);
        }

        // GET: Create User
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        // POST: Create User
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(
            string userName,
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "All fields are required.";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["Error"] =
                    "Password must be at least 6 characters long.";

                return View();
            }

            var user = new IdentityUser
            {
                UserName = userName.Trim(),
                Email = email.Trim(),
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                TempData["Success"] = "User created successfully.";
                return RedirectToAction(nameof(Users));
            }

            TempData["Error"] = string.Join(
                " ",
                result.Errors.Select(e => e.Description));

            return View();
        }

        // GET: Delete User confirmation
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Delete User
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                TempData["Success"] = "User deleted successfully.";
            else
                TempData["Error"] = string.Join(
                    " ",
                    result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Users));
        }

        // =====================================================
        // ROLES MANAGEMENT
        // =====================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Roles(string searchString)

        {
            var roles = _roleManager.Roles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                roles = roles.Where(r =>
                    r.Name != null && r.Name.Contains(searchString));
            }

            ViewBag.SearchString = searchString;

            var roleList = await roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(roleList);
        }

        // GET: Create Role
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateRole()
        {
            return View();
        }

        // POST: Create Role
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Role name is required.";
                return View();
            }

            roleName = roleName.Trim();

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["Error"] = "This role already exists.";
                return View();
            }

            var result = await _roleManager.CreateAsync(
                new IdentityRole(roleName));

            if (result.Succeeded)
            {
                TempData["Success"] = "Role created successfully.";
                return RedirectToAction(nameof(Roles));
            }

            TempData["Error"] = string.Join(
                " ",
                result.Errors.Select(e => e.Description));

            return View();
        }

        // GET: Delete Role confirmation
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> DeleteRole(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound();

            return View(role);
        }

        // POST: Delete Role
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoleConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            if (role.Name == "Admin")
            {
                TempData["Error"] = "The Admin role cannot be deleted.";
                return RedirectToAction(nameof(Roles));
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(
                role.Name ?? string.Empty);

            if (usersInRole.Count > 0)
            {
                TempData["Error"] =
                    "This role has assigned users. Remove their role first.";

                return RedirectToAction(nameof(Roles));
            }

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                TempData["Success"] = "Role deleted successfully.";
            }
            else
            {
                TempData["Error"] = string.Join(
                    " ",
                    result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Roles));



        }

        // =====================================================
        // PERMISSIONS MANAGEMENT
        // =====================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Permissions(string searchString)


        {
            var permissions = _context.Permissions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                permissions = permissions.Where(p =>
                    p.Name.Contains(searchString) ||
                    (p.Description != null &&
                     p.Description.Contains(searchString)));
            }

            ViewBag.SearchString = searchString;

            var list = await permissions
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Permission
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreatePermission()
        {
            return View();
        }

        // POST: Create Permission
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePermission(Permission permission)
        {
            if (!ModelState.IsValid)
                return View(permission);

            bool exists = await _context.Permissions.AnyAsync(p =>
                p.Name.ToLower() == permission.Name.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(permission.Name),
                    "This permission already exists.");

                return View(permission);
            }

            permission.Name = permission.Name.Trim();
            permission.Description = permission.Description?.Trim();
            permission.IsActive = true;

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Permission created successfully.";

            return RedirectToAction(nameof(Permissions));
        }

        // GET: Edit Permission
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditPermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);

            if (permission == null)
                return NotFound();

            return View(permission);
        }

        // POST: Edit Permission
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPermission(
            int id, Permission permission)
        {
            if (id != permission.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(permission);

            string name = permission.Name.Trim();

            bool exists = await _context.Permissions.AnyAsync(p =>
                p.Id != id &&
                p.Name.ToLower() == name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(permission.Name),
                    "This permission name already exists.");

                return View(permission);
            }

            var existing = await _context.Permissions.FindAsync(id);

            if (existing == null)
                return NotFound();

            existing.Name = name;
            existing.Description = permission.Description?.Trim();
            existing.IsActive = permission.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Permission updated successfully.";

            return RedirectToAction(nameof(Permissions));
        }

        // POST: Delete Permission
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);

            if (permission == null)
                return NotFound();

            bool isAssigned = await _context.RolePermissions
                .AnyAsync(rp => rp.PermissionId == id);

            if (isAssigned)
            {
                TempData["Error"] =
                    "This permission is assigned to a role. Remove its role mapping first.";

                return RedirectToAction(nameof(Permissions));
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Permission deleted successfully.";

            return RedirectToAction(nameof(Permissions));
        }



        // =====================================================
        // ROLE PERMISSIONS MAPPING
        // =====================================================

        // GET: View and manage permissions for a role
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> RolePermissions(string roleId)

        {
            if (string.IsNullOrWhiteSpace(roleId))
                return BadRequest();

            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null)
                return NotFound();

            ViewBag.RoleId = role.Id;
            ViewBag.RoleName = role.Name;

            var permissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            var mappings = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            ViewBag.AssignedPermissions = mappings;

            return View(permissions);
        }

        // POST: Save permissions for a role
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRolePermissions(
            string roleId,
            int[] permissionIds)
        {
            if (string.IsNullOrWhiteSpace(roleId))
                return BadRequest();

            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null)
                return NotFound();

            permissionIds ??= Array.Empty<int>();

            var validPermissionIds = await _context.Permissions
                .Where(p => p.IsActive && permissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            var existingMappings = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            foreach (var mapping in existingMappings)
            {
                mapping.IsAllowed = validPermissionIds.Contains(
                    mapping.PermissionId);
            }

            var existingPermissionIds = existingMappings
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            foreach (var permissionId in validPermissionIds)
            {
                if (!existingPermissionIds.Contains(permissionId))
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        IsAllowed = true
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Role permissions saved successfully.";

            return RedirectToAction(nameof(RolePermissions),
                new { roleId });
        }


        // =====================================================
        // PAGES MANAGEMENT
        // =====================================================

        // GET: Pages list with search and status filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Pages(
            string? searchString,
            string? status)
        {
            var pages = _context.Pages.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                pages = pages.Where(p =>
                    p.Title.Contains(searchString) ||
                    p.Slug.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                pages = pages.Where(p => p.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            var list = await pages
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Page
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreatePage()
        {
            return View(new Page());
        }

        // POST: Create Page
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePage(Page page)
        {
            page.Title = page.Title?.Trim() ?? string.Empty;
            page.Slug = page.Slug?.Trim().Trim('/') ?? string.Empty;

            if (string.IsNullOrWhiteSpace(page.Slug))
            {
                ModelState.AddModelError(
                    nameof(page.Slug), "Slug is required.");
            }

            bool slugExists = !string.IsNullOrWhiteSpace(page.Slug)
                && await _context.Pages.AnyAsync(p =>
                    p.Slug.ToLower() == page.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(page.Slug), "This slug already exists.");
            }

            if (!ModelState.IsValid)
                return View(page);

            page.Status = string.IsNullOrWhiteSpace(page.Status)
                ? "Draft"
                : page.Status.Trim();

            page.IsPublished = page.Status.Equals(
                "Published", StringComparison.OrdinalIgnoreCase);

            page.CreatedAt = DateTime.UtcNow;
            page.UpdatedAt = null;

            _context.Pages.Add(page);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Page created successfully.";

            return RedirectToAction(nameof(Pages));
        }

        // GET: Edit Page
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditPage(int id)
        {
            var page = await _context.Pages.FindAsync(id);

            if (page == null)
                return NotFound();

            return View(page);
        }

        // POST: Edit Page
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPage(int id, Page page)
        {
            if (id != page.Id)
                return BadRequest();

            page.Title = page.Title?.Trim() ?? string.Empty;
            page.Slug = page.Slug?.Trim().Trim('/') ?? string.Empty;

            bool slugExists = !string.IsNullOrWhiteSpace(page.Slug)
                && await _context.Pages.AnyAsync(p =>
                    p.Id != id &&
                    p.Slug.ToLower() == page.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(page.Slug), "This slug already exists.");
            }

            if (!ModelState.IsValid)
                return View(page);

            var existingPage = await _context.Pages.FindAsync(id);

            if (existingPage == null)
                return NotFound();

            existingPage.Title = page.Title;
            existingPage.Slug = page.Slug;
            existingPage.Content = page.Content;
            existingPage.Status = string.IsNullOrWhiteSpace(page.Status)
                ? "Draft"
                : page.Status.Trim();
            existingPage.IsPublished = existingPage.Status.Equals(
                "Published", StringComparison.OrdinalIgnoreCase);
            existingPage.MetaTitle = page.MetaTitle;
            existingPage.MetaDescription = page.MetaDescription;
            existingPage.IsVisible = page.IsVisible;
            existingPage.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Page updated successfully.";

            return RedirectToAction(nameof(Pages));
        }

        // GET: Page Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> PageDetails(int id)
        {
            var page = await _context.Pages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (page == null)
                return NotFound();

            return View(page);
        }

        // POST: Delete Page
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePage(int id)
        {
            var page = await _context.Pages.FindAsync(id);

            if (page == null)
                return NotFound();

            _context.Pages.Remove(page);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Page deleted successfully.";

            return RedirectToAction(nameof(Pages));
        }
        // =====================================================
        // BLOGS MANAGEMENT
        // =====================================================

        // GET: Blogs List, Search and Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Blogs(
            string? searchString,
            string? status)
        {
            var blogs = _context.Blogs
                .AsNoTracking()
                .Include(b => b.BlogCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                blogs = blogs.Where(b =>
                    b.Title.Contains(searchString) ||
                    b.Slug.Contains(searchString) ||
                    (b.Author != null && b.Author.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                blogs = blogs.Where(b => b.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            var list = await blogs
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // Load categories for Create/Edit views
        private async Task LoadBlogCategoriesAsync(
            int? selectedCategoryId = null)
        {
            var categories = await _context.BlogCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.BlogCategories =
                new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    categories,
                    "Id",
                    "Name",
                    selectedCategoryId);
        }

        // GET: Create Blog
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> CreateBlog()
        {
            await LoadBlogCategoriesAsync();
            return View(new Blog());
        }

        // POST: Create Blog
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlog(Blog blog)
        {
            blog.Title = blog.Title?.Trim() ?? string.Empty;
            blog.Slug = blog.Slug?.Trim().Trim('/') ?? string.Empty;

            if (string.IsNullOrWhiteSpace(blog.Slug))
            {
                ModelState.AddModelError(
                    nameof(blog.Slug), "Slug is required.");
            }

            bool slugExists = !string.IsNullOrWhiteSpace(blog.Slug)
                && await _context.Blogs.AnyAsync(b =>
                    b.Slug.ToLower() == blog.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(blog.Slug), "This blog slug already exists.");
            }

            if (blog.BlogCategoryId.HasValue)
            {
                bool categoryExists = await _context.BlogCategories
                    .AnyAsync(c => c.Id == blog.BlogCategoryId.Value);

                if (!categoryExists)
                {
                    ModelState.AddModelError(
                        nameof(blog.BlogCategoryId),
                        "Please select a valid category.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadBlogCategoriesAsync(blog.BlogCategoryId);
                return View(blog);
            }

            blog.Status = string.IsNullOrWhiteSpace(blog.Status)
                ? "Draft"
                : blog.Status.Trim();

            blog.IsPublished = blog.Status.Equals(
                "Published", StringComparison.OrdinalIgnoreCase);

            if (blog.IsPublished && !blog.PublicationDate.HasValue)
                blog.PublicationDate = DateTime.UtcNow;

            if (!blog.IsPublished)
                blog.PublicationDate = null;

            blog.CreatedAt = DateTime.UtcNow;
            blog.UpdatedAt = null;

            // If SanitizeBlogHtml already exists inside this controller,
            // sanitize the rich-text content before saving.
            blog.Content = SanitizeBlogHtml(blog.Content);

            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog created successfully.";

            return RedirectToAction(nameof(Blogs));
        }

        // GET: Edit Blog
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditBlog(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);

            if (blog == null)
                return NotFound();

            await LoadBlogCategoriesAsync(blog.BlogCategoryId);

            return View(blog);
        }

        // POST: Edit Blog
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlog(int id, Blog blog)
        {
            if (id != blog.Id)
                return BadRequest();

            blog.Title = blog.Title?.Trim() ?? string.Empty;
            blog.Slug = blog.Slug?.Trim().Trim('/') ?? string.Empty;

            bool slugExists = !string.IsNullOrWhiteSpace(blog.Slug)
                && await _context.Blogs.AnyAsync(b =>
                    b.Id != id &&
                    b.Slug.ToLower() == blog.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(blog.Slug), "This blog slug already exists.");
            }

            if (blog.BlogCategoryId.HasValue)
            {
                bool categoryExists = await _context.BlogCategories
                    .AnyAsync(c => c.Id == blog.BlogCategoryId.Value);

                if (!categoryExists)
                {
                    ModelState.AddModelError(
                        nameof(blog.BlogCategoryId),
                        "Please select a valid category.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadBlogCategoriesAsync(blog.BlogCategoryId);
                return View(blog);
            }

            var existingBlog = await _context.Blogs.FindAsync(id);

            if (existingBlog == null)
                return NotFound();

            existingBlog.Title = blog.Title;
            existingBlog.Slug = blog.Slug;
            existingBlog.Excerpt = blog.Excerpt;
            existingBlog.Content = SanitizeBlogHtml(blog.Content);
            existingBlog.FeaturedImage = blog.FeaturedImage;
            existingBlog.Author = blog.Author;
            existingBlog.BlogCategoryId = blog.BlogCategoryId;
            existingBlog.Tags = blog.Tags;
            existingBlog.Status = string.IsNullOrWhiteSpace(blog.Status)
                ? "Draft"
                : blog.Status.Trim();
            existingBlog.IsPublished = existingBlog.Status.Equals(
                "Published", StringComparison.OrdinalIgnoreCase);
            existingBlog.IsFeatured = blog.IsFeatured;
            existingBlog.MetaTitle = blog.MetaTitle;
            existingBlog.MetaDescription = blog.MetaDescription;

            if (existingBlog.IsPublished)
            {
                if (!existingBlog.PublicationDate.HasValue)
                    existingBlog.PublicationDate = DateTime.UtcNow;
            }
            else
            {
                existingBlog.PublicationDate = null;
            }

            existingBlog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog updated successfully.";

            return RedirectToAction(nameof(Blogs));
        }

        // GET: Blog Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> BlogDetails(int id)
        {
            var blog = await _context.Blogs
                .AsNoTracking()
                .Include(b => b.BlogCategory)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (blog == null)
                return NotFound();

            return View(blog);
        }

        // POST: Delete Blog
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);

            if (blog == null)
                return NotFound();

            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog deleted successfully.";

            return RedirectToAction(nameof(Blogs));
        }

        // POST: Publish Blog
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishBlog(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);

            if (blog == null)
                return NotFound();

            blog.Status = "Published";
            blog.IsPublished = true;
            blog.PublicationDate ??= DateTime.UtcNow;
            blog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog published successfully.";

            return RedirectToAction(nameof(Blogs));
        }

        // POST: Move Blog to Draft
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpublishBlog(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);

            if (blog == null)
                return NotFound();

            blog.Status = "Draft";
            blog.IsPublished = false;
            blog.PublicationDate = null;
            blog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog moved to Draft successfully.";

            return RedirectToAction(nameof(Blogs));
        }
        // =====================================================
        // BLOG HTML SANITIZER
        // =====================================================
        private string? SanitizeBlogHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            var sanitizer = new HtmlSanitizer();

            return sanitizer.Sanitize(html);
        }


        // =====================================================
        // BLOG CATEGORIES MANAGEMENT
        // =====================================================

        // GET: Blog Categories List + Search
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> BlogCategories(string? searchString)
        {
            var categories = _context.BlogCategories
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                categories = categories.Where(c =>
                    c.Name.Contains(searchString) ||
                    (c.Slug != null && c.Slug.Contains(searchString)));
            }

            ViewBag.SearchString = searchString;

            var list = await categories
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Blog Category
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateBlogCategory()
        {
            return View(new BlogCategory());
        }

        // POST: Create Blog Category
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlogCategory(BlogCategory category)
        {
            category.Name = category.Name?.Trim() ?? string.Empty;
            category.Slug = string.IsNullOrWhiteSpace(category.Slug)
                ? null
                : category.Slug.Trim().Trim('/');

            bool nameExists = await _context.BlogCategories.AnyAsync(c =>
                c.Name.ToLower() == category.Name.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "This category name already exists.");
            }

            if (!string.IsNullOrWhiteSpace(category.Slug))
            {
                bool slugExists = await _context.BlogCategories.AnyAsync(c =>
                    c.Slug != null &&
                    c.Slug.ToLower() == category.Slug.ToLower());

                if (slugExists)
                {
                    ModelState.AddModelError(
                        nameof(category.Slug),
                        "This category slug already exists.");
                }
            }

            if (!ModelState.IsValid)
                return View(category);

            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = null;

            _context.BlogCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog category created successfully.";

            return RedirectToAction(nameof(BlogCategories));
        }

        // GET: Edit Blog Category
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditBlogCategory(int id)
        {
            var category = await _context.BlogCategories.FindAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: Edit Blog Category
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlogCategory(
            int id, BlogCategory category)
        {
            if (id != category.Id)
                return BadRequest();

            category.Name = category.Name?.Trim() ?? string.Empty;
            category.Slug = string.IsNullOrWhiteSpace(category.Slug)
                ? null
                : category.Slug.Trim().Trim('/');

            bool nameExists = await _context.BlogCategories.AnyAsync(c =>
                c.Id != id &&
                c.Name.ToLower() == category.Name.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "This category name already exists.");
            }

            if (!string.IsNullOrWhiteSpace(category.Slug))
            {
                bool slugExists = await _context.BlogCategories.AnyAsync(c =>
                    c.Id != id &&
                    c.Slug != null &&
                    c.Slug.ToLower() == category.Slug.ToLower());

                if (slugExists)
                {
                    ModelState.AddModelError(
                        nameof(category.Slug),
                        "This category slug already exists.");
                }
            }

            if (!ModelState.IsValid)
                return View(category);

            var existingCategory = await _context.BlogCategories.FindAsync(id);

            if (existingCategory == null)
                return NotFound();

            existingCategory.Name = category.Name;
            existingCategory.Slug = category.Slug;
            existingCategory.Description = category.Description;
            existingCategory.IsActive = category.IsActive;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog category updated successfully.";

            return RedirectToAction(nameof(BlogCategories));
        }

        // GET: Blog Category Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> BlogCategoryDetails(int id)
        {
            var category = await _context.BlogCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            ViewBag.BlogCount = await _context.Blogs
                .CountAsync(b => b.BlogCategoryId == id);

            return View(category);
        }


        // GET: Delete Blog Category confirmation page
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> DeleteBlogCategory(int id)
        {
            var category = await _context.BlogCategories.FindAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }


        // POST: Delete Blog Category
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlogCategoryConfirmed(int id)
        {
            var category = await _context.BlogCategories.FindAsync(id);

            if (category == null)
                return NotFound();

            bool hasBlogs = await _context.Blogs
                .AnyAsync(b => b.BlogCategoryId == id);

            if (hasBlogs)
            {
                TempData["Error"] =
                    "This category cannot be deleted because blogs are assigned to it.";

                return RedirectToAction(nameof(BlogCategories));
            }

            _context.BlogCategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog category deleted successfully.";

            return RedirectToAction(nameof(BlogCategories));
        }

        // =====================================================
        // BLOG TAGS MANAGEMENT
        // =====================================================

        // GET: Blog Tags List + Search
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> BlogTags(string? searchString)
        {
            var tags = _context.BlogTags
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                tags = tags.Where(t =>
                    t.Name.Contains(searchString) ||
                    t.Slug.Contains(searchString));
            }

            ViewBag.SearchString = searchString;

            var list = await tags
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Blog Tag
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateBlogTag()
        {
            return View(new BlogTag());
        }

        // POST: Create Blog Tag
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlogTag(BlogTag tag)
        {
            tag.Name = tag.Name?.Trim() ?? string.Empty;
            tag.Slug = tag.Slug?.Trim().Trim('/') ?? string.Empty;

            bool nameExists = await _context.BlogTags.AnyAsync(t =>
                t.Name.ToLower() == tag.Name.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(tag.Name),
                    "This tag name already exists.");
            }

            bool slugExists = !string.IsNullOrWhiteSpace(tag.Slug)
                && await _context.BlogTags.AnyAsync(t =>
                    t.Slug.ToLower() == tag.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(tag.Slug),
                    "This tag slug already exists.");
            }

            if (!ModelState.IsValid)
                return View(tag);

            tag.CreatedAt = DateTime.UtcNow;
            tag.UpdatedAt = null;

            _context.BlogTags.Add(tag);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog tag created successfully.";

            return RedirectToAction(nameof(BlogTags));
        }

        // GET: Edit Blog Tag
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditBlogTag(int id)
        {
            var tag = await _context.BlogTags.FindAsync(id);

            if (tag == null)
                return NotFound();

            return View(tag);
        }

        // POST: Edit Blog Tag
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlogTag(int id, BlogTag tag)
        {
            if (id != tag.Id)
                return BadRequest();

            tag.Name = tag.Name?.Trim() ?? string.Empty;
            tag.Slug = tag.Slug?.Trim().Trim('/') ?? string.Empty;

            bool nameExists = await _context.BlogTags.AnyAsync(t =>
                t.Id != id &&
                t.Name.ToLower() == tag.Name.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(tag.Name),
                    "This tag name already exists.");
            }

            bool slugExists = !string.IsNullOrWhiteSpace(tag.Slug)
                && await _context.BlogTags.AnyAsync(t =>
                    t.Id != id &&
                    t.Slug.ToLower() == tag.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(tag.Slug),
                    "This tag slug already exists.");
            }

            if (!ModelState.IsValid)
                return View(tag);

            var existingTag = await _context.BlogTags.FindAsync(id);

            if (existingTag == null)
                return NotFound();

            existingTag.Name = tag.Name;
            existingTag.Slug = tag.Slug;
            existingTag.IsActive = tag.IsActive;
            existingTag.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog tag updated successfully.";

            return RedirectToAction(nameof(BlogTags));
        }

        // GET: Blog Tag Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> BlogTagDetails(int id)
        {
            var tag = await _context.BlogTags
                .AsNoTracking()
                .Include(t => t.Blogs)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null)
                return NotFound();

            return View(tag);
        }

        // POST: Delete Blog Tag
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlogTag(int id)
        {
            var tag = await _context.BlogTags
                .Include(t => t.Blogs)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null)
                return NotFound();

            // Remove tag references from blogs before deleting the tag.
            // This preserves the blog records.
            foreach (var blog in tag.Blogs.ToList())
            {
                blog.BlogTags.Remove(tag);
            }

            _context.BlogTags.Remove(tag);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog tag deleted successfully.";

            return RedirectToAction(nameof(BlogTags));
        }

        // =====================================================
        // PROJECTS MANAGEMENT
        // =====================================================

        // GET: Projects List + Search + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Projects(
            string? searchString,
            string? status)
        {
            var projects = _context.Projects
                .AsNoTracking()
                .Include(p => p.ProjectCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                projects = projects.Where(p =>
                    p.Title.Contains(searchString) ||
                    p.Slug.Contains(searchString) ||
                    (p.ClientName != null &&
                     p.ClientName.Contains(searchString)) ||
                    (p.ProjectType != null &&
                     p.ProjectType.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                projects = projects.Where(p => p.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            var list = await projects
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // Load Project Categories Dropdown
        private async Task LoadProjectCategoriesAsync(
            int? selectedCategoryId = null)
        {
            var categories = await _context.ProjectCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.ProjectCategories =
                new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    categories,
                    "Id",
                    "Name",
                    selectedCategoryId);
        }

        // GET: Create Project
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> CreateProject()
        {
            await LoadProjectCategoriesAsync();
            return View(new Project());
        }

        // POST: Create Project
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(Project project)
        {
            project.Title = project.Title?.Trim() ?? string.Empty;
            project.Slug = project.Slug?.Trim().Trim('/') ?? string.Empty;

            bool slugExists = !string.IsNullOrWhiteSpace(project.Slug)
                && await _context.Projects.AnyAsync(p =>
                    p.Slug.ToLower() == project.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(project.Slug),
                    "This project slug already exists.");
            }

            if (project.ProjectCategoryId.HasValue)
            {
                bool categoryExists = await _context.ProjectCategories
                    .AnyAsync(c => c.Id == project.ProjectCategoryId.Value);

                if (!categoryExists)
                {
                    ModelState.AddModelError(
                        nameof(project.ProjectCategoryId),
                        "Please select a valid project category.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadProjectCategoriesAsync(project.ProjectCategoryId);
                return View(project);
            }

            project.Status = string.IsNullOrWhiteSpace(project.Status)
                ? "Draft"
                : project.Status.Trim();

            project.CreatedAt = DateTime.UtcNow;
            project.UpdatedAt = null;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project created successfully.";

            return RedirectToAction(nameof(Projects));
        }

        // GET: Edit Project
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
                return NotFound();

            await LoadProjectCategoriesAsync(project.ProjectCategoryId);

            return View(project);
        }

        // POST: Edit Project
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(int id, Project project)
        {
            if (id != project.Id)
                return BadRequest();

            project.Title = project.Title?.Trim() ?? string.Empty;
            project.Slug = project.Slug?.Trim().Trim('/') ?? string.Empty;

            bool slugExists = !string.IsNullOrWhiteSpace(project.Slug)
                && await _context.Projects.AnyAsync(p =>
                    p.Id != id &&
                    p.Slug.ToLower() == project.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(project.Slug),
                    "This project slug already exists.");
            }

            if (project.ProjectCategoryId.HasValue)
            {
                bool categoryExists = await _context.ProjectCategories
                    .AnyAsync(c => c.Id == project.ProjectCategoryId.Value);

                if (!categoryExists)
                {
                    ModelState.AddModelError(
                        nameof(project.ProjectCategoryId),
                        "Please select a valid project category.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadProjectCategoriesAsync(project.ProjectCategoryId);
                return View(project);
            }

            var existingProject = await _context.Projects.FindAsync(id);

            if (existingProject == null)
                return NotFound();

            existingProject.Title = project.Title;
            existingProject.Slug = project.Slug;
            existingProject.ShortDescription = project.ShortDescription;
            existingProject.Description = project.Description;
            existingProject.ClientName = project.ClientName;
            existingProject.ProjectType = project.ProjectType;
            existingProject.ProjectCategoryId = project.ProjectCategoryId;
            existingProject.FeaturedImage = project.FeaturedImage;
            existingProject.Technologies = project.Technologies;
            existingProject.ProjectUrl = project.ProjectUrl;
            existingProject.Status = string.IsNullOrWhiteSpace(project.Status)
                ? "Draft"
                : project.Status.Trim();
            existingProject.IsFeatured = project.IsFeatured;
            existingProject.ProjectDate = project.ProjectDate;
            existingProject.MetaTitle = project.MetaTitle;
            existingProject.MetaDescription = project.MetaDescription;
            existingProject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Project updated successfully.";

            return RedirectToAction(nameof(Projects));
        }

        // GET: Project Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ProjectDetails(int id)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .Include(p => p.ProjectCategory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        // POST: Delete Project
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
                return NotFound();

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project deleted successfully.";

            return RedirectToAction(nameof(Projects));
        }

        // POST: Publish Project
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
                return NotFound();

            project.Status = "Published";
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Project published successfully.";

            return RedirectToAction(nameof(Projects));
        }

        // POST: Move Project to Draft
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpublishProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
                return NotFound();

            project.Status = "Draft";
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Project moved to Draft successfully.";

            return RedirectToAction(nameof(Projects));
        }

        // =====================================================
        // PROJECT CATEGORIES MANAGEMENT
        // =====================================================

        // GET: Project Categories List + Search
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ProjectCategories(string? searchString)
        {
            var categories = _context.ProjectCategories
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                categories = categories.Where(c =>
                    c.Name.Contains(searchString) ||
                    c.Slug.Contains(searchString));
            }

            ViewBag.SearchString = searchString;

            var list = await categories
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Project Category
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateProjectCategory()
        {
            return View(new ProjectCategory());
        }

        // POST: Create Project Category
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProjectCategory(
            ProjectCategory category)
        {
            category.Name = category.Name?.Trim() ?? string.Empty;
            category.Slug = category.Slug?.Trim().Trim('/') ?? string.Empty;

            bool nameExists = await _context.ProjectCategories.AnyAsync(c =>
                c.Name.ToLower() == category.Name.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "This category name already exists.");
            }

            bool slugExists = !string.IsNullOrWhiteSpace(category.Slug)
                && await _context.ProjectCategories.AnyAsync(c =>
                    c.Slug.ToLower() == category.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(category.Slug),
                    "This category slug already exists.");
            }

            if (!ModelState.IsValid)
                return View(category);

            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = null;

            _context.ProjectCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project category created successfully.";

            return RedirectToAction(nameof(ProjectCategories));
        }

        // GET: Edit Project Category
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditProjectCategory(int id)
        {
            var category = await _context.ProjectCategories.FindAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: Edit Project Category
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProjectCategory(
            int id, ProjectCategory category)
        {
            if (id != category.Id)
                return BadRequest();

            category.Name = category.Name?.Trim() ?? string.Empty;
            category.Slug = category.Slug?.Trim().Trim('/') ?? string.Empty;

            bool nameExists = await _context.ProjectCategories.AnyAsync(c =>
                c.Id != id &&
                c.Name.ToLower() == category.Name.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "This category name already exists.");
            }

            bool slugExists = !string.IsNullOrWhiteSpace(category.Slug)
                && await _context.ProjectCategories.AnyAsync(c =>
                    c.Id != id &&
                    c.Slug.ToLower() == category.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(category.Slug),
                    "This category slug already exists.");
            }

            if (!ModelState.IsValid)
                return View(category);

            var existingCategory =
                await _context.ProjectCategories.FindAsync(id);

            if (existingCategory == null)
                return NotFound();

            existingCategory.Name = category.Name;
            existingCategory.Slug = category.Slug;
            existingCategory.Description = category.Description;
            existingCategory.IsActive = category.IsActive;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Project category updated successfully.";

            return RedirectToAction(nameof(ProjectCategories));
        }

        // GET: Project Category Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ProjectCategoryDetails(int id)
        {
            var category = await _context.ProjectCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            ViewBag.ProjectCount = await _context.Projects
                .CountAsync(p => p.ProjectCategoryId == id);

            return View(category);
        }

        // POST: Delete Project Category
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProjectCategory(int id)
        {
            var category = await _context.ProjectCategories.FindAsync(id);

            if (category == null)
                return NotFound();

            bool hasProjects = await _context.Projects
                .AnyAsync(p => p.ProjectCategoryId == id);

            if (hasProjects)
            {
                TempData["Error"] =
                    "This category cannot be deleted because projects are assigned to it.";

                return RedirectToAction(nameof(ProjectCategories));
            }

            _context.ProjectCategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project category deleted successfully.";

            return RedirectToAction(nameof(ProjectCategories));
        }

        // =====================================================
        // GALLERY MANAGEMENT
        // =====================================================

        // GET: Gallery List + Search + Category Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Gallery(
            string? searchString,
            string? category)
        {
            var items = _context.Galleries
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                items = items.Where(g =>
                    g.Title.Contains(searchString) ||
                    (g.Description != null &&
                     g.Description.Contains(searchString)) ||
                    (g.Category != null &&
                     g.Category.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                items = items.Where(g => g.Category == category);
            }

            ViewBag.SearchString = searchString;
            ViewBag.SelectedCategory = category;

            ViewBag.Categories = await _context.Galleries
                .Where(g => g.Category != null && g.Category != "")
                .Select(g => g.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var list = await items
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Gallery Item
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateGallery()
        {
            return View(new Gallery());
        }

        // POST: Create Gallery Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGallery(Gallery gallery)
        {
            gallery.Title = gallery.Title?.Trim() ?? string.Empty;
            gallery.ImageUrl = gallery.ImageUrl?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(gallery.ImageUrl) &&
                !Uri.TryCreate(
                    gallery.ImageUrl,
                    UriKind.Absolute,
                    out Uri? imageUri))
            {
                ModelState.AddModelError(
                    nameof(gallery.ImageUrl),
                    "Please enter a valid image URL.");
            }
            else if (Uri.TryCreate(
                         gallery.ImageUrl,
                         UriKind.Absolute,
                         out Uri? validImageUri) &&
                     validImageUri.Scheme != Uri.UriSchemeHttp &&
                     validImageUri.Scheme != Uri.UriSchemeHttps)
            {
                ModelState.AddModelError(
                    nameof(gallery.ImageUrl),
                    "Image URL must use HTTP or HTTPS.");
            }

            if (!ModelState.IsValid)
                return View(gallery);

            gallery.CreatedAt = DateTime.UtcNow;
            gallery.UpdatedAt = null;

            _context.Galleries.Add(gallery);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gallery item created successfully.";

            return RedirectToAction(nameof(Gallery));
        }

        // GET: Edit Gallery Item
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditGallery(int id)
        {
            var gallery = await _context.Galleries.FindAsync(id);

            if (gallery == null)
                return NotFound();

            return View(gallery);
        }

        // POST: Edit Gallery Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGallery(int id, Gallery gallery)
        {
            if (id != gallery.Id)
                return BadRequest();

            gallery.Title = gallery.Title?.Trim() ?? string.Empty;
            gallery.ImageUrl = gallery.ImageUrl?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(gallery.ImageUrl) &&
                Uri.TryCreate(
                    gallery.ImageUrl,
                    UriKind.Absolute,
                    out Uri? imageUri) &&
                imageUri.Scheme != Uri.UriSchemeHttp &&
                imageUri.Scheme != Uri.UriSchemeHttps)
            {
                ModelState.AddModelError(
                    nameof(gallery.ImageUrl),
                    "Image URL must use HTTP or HTTPS.");
            }
            else if (!string.IsNullOrWhiteSpace(gallery.ImageUrl) &&
                     !Uri.TryCreate(
                         gallery.ImageUrl,
                         UriKind.Absolute,
                         out _))
            {
                ModelState.AddModelError(
                    nameof(gallery.ImageUrl),
                    "Please enter a valid image URL.");
            }

            if (!ModelState.IsValid)
                return View(gallery);

            var existingGallery = await _context.Galleries.FindAsync(id);

            if (existingGallery == null)
                return NotFound();

            existingGallery.Title = gallery.Title;
            existingGallery.Description = gallery.Description;
            existingGallery.ImageUrl = gallery.ImageUrl;
            existingGallery.Category = gallery.Category;
            existingGallery.IsActive = gallery.IsActive;
            existingGallery.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Gallery item updated successfully.";

            return RedirectToAction(nameof(Gallery));
        }

        // GET: Gallery Item Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GalleryDetails(int id)
        {
            var gallery = await _context.Galleries
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gallery == null)
                return NotFound();

            return View(gallery);
        }

        // POST: Delete Gallery Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGallery(int id)
        {
            var gallery = await _context.Galleries.FindAsync(id);

            if (gallery == null)
                return NotFound();

            _context.Galleries.Remove(gallery);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gallery item deleted successfully.";

            return RedirectToAction(nameof(Gallery));
        }

        // POST: Activate Gallery Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateGallery(int id)
        {
            var gallery = await _context.Galleries.FindAsync(id);

            if (gallery == null)
                return NotFound();

            gallery.IsActive = true;
            gallery.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Gallery item activated successfully.";

            return RedirectToAction(nameof(Gallery));
        }

        // POST: Deactivate Gallery Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateGallery(int id)
        {
            var gallery = await _context.Galleries.FindAsync(id);

            if (gallery == null)
                return NotFound();

            gallery.IsActive = false;
            gallery.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Gallery item deactivated successfully.";

            return RedirectToAction(nameof(Gallery));
        }

        // =====================================================
        // MEDIA LIBRARY MANAGEMENT
        // =====================================================

        // GET: Media List + Search + Category + File Type Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> MediaLibrary(
            string? searchString,
            string? category,
            string? fileType)
        {
            var media = _context.Media
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                media = media.Where(m =>
                    m.Title.Contains(searchString) ||
                    m.FileName.Contains(searchString) ||
                    (m.Description != null &&
                     m.Description.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(category))
                media = media.Where(m => m.Category == category);

            if (!string.IsNullOrWhiteSpace(fileType))
                media = media.Where(m => m.FileType == fileType);

            ViewBag.SearchString = searchString;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedFileType = fileType;

            ViewBag.Categories = await _context.Media
                .Where(m => m.Category != null && m.Category != "")
                .Select(m => m.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.FileTypes = await _context.Media
                .Where(m => m.FileType != null && m.FileType != "")
                .Select(m => m.FileType!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            var list = await media
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View("Media", list);
        }

        // GET: Create Media Item
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateMedia()
        {
            return View(new Media());
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMedia(
   Media media,
   IFormFile? uploadFile)
        {
            media.Title = media.Title?.Trim() ?? string.Empty;

            const long maxFileSize = 5 * 1024 * 1024;

            var allowedExtensions = new[]
            {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx",
        ".txt", ".mp4", ".mp3"
    };

            string? storedFileName = null;

            if (uploadFile == null || uploadFile.Length == 0)
            {
                ModelState.AddModelError(
                    "uploadFile",
                    "Please select a file to upload.");
            }
            else if (uploadFile.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "uploadFile",
                    "File size must not exceed 5 MB.");
            }
            else
            {
                var extension = Path.GetExtension(uploadFile.FileName)
                    .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "uploadFile",
                        "This file type is not allowed.");
                }
                else
                {
                    storedFileName = $"{Guid.NewGuid():N}{extension}";

                    media.FileName = Path.GetFileName(uploadFile.FileName);
                    media.FileUrl = $"/uploads/media/{storedFileName}";
                    media.FileType = extension;
                    media.FileSize = uploadFile.Length;
                }
            }

            // FileName and FileUrl are generated from the uploaded file.
            ModelState.Remove(nameof(Media.FileName));
            ModelState.Remove(nameof(Media.FileUrl));

            TryValidateModel(media);

            if (!ModelState.IsValid)
            {
                return View(media);
            }

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            var uploadFolder = Path.Combine(
                webRoot, "uploads", "media");

            Directory.CreateDirectory(uploadFolder);

            var filePath = Path.Combine(uploadFolder, storedFileName!);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.CreateNew))
                {
                    await uploadFile!.CopyToAsync(stream);
                }

                media.CreatedAt = DateTime.UtcNow;
                media.UpdatedAt = null;

                _context.Media.Add(media);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Media item created successfully.";

                return RedirectToAction(nameof(MediaLibrary));
            }
            catch
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }
        }

        // GET: Edit Media Item
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditMedia(int id)
        {
            var media = await _context.Media.FindAsync(id);

            if (media == null)
                return NotFound();

            return View(media);
        }

        // POST: Edit Media Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedia(int id, Media media)
        {
            if (id != media.Id)
                return BadRequest();

            media.Title = media.Title?.Trim() ?? string.Empty;
            media.FileName = media.FileName?.Trim() ?? string.Empty;
            media.FileUrl = media.FileUrl?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(media.FileUrl))
            {
                if (!Uri.TryCreate(
                        media.FileUrl,
                        UriKind.Absolute,
                        out Uri? fileUri))
                {
                    ModelState.AddModelError(
                        nameof(media.FileUrl),
                        "Please enter a valid file URL.");
                }
                else if (fileUri.Scheme != Uri.UriSchemeHttp &&
                         fileUri.Scheme != Uri.UriSchemeHttps)
                {
                    ModelState.AddModelError(
                        nameof(media.FileUrl),
                        "File URL must use HTTP or HTTPS.");
                }
            }

            if (media.FileSize < 0)
            {
                ModelState.AddModelError(
                    nameof(media.FileSize),
                    "File size cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(media);

            var existingMedia = await _context.Media.FindAsync(id);

            if (existingMedia == null)
                return NotFound();

            existingMedia.Title = media.Title;
            existingMedia.Description = media.Description;
            existingMedia.FileName = media.FileName;
            existingMedia.FileUrl = media.FileUrl;
            existingMedia.FileType = media.FileType;
            existingMedia.FileSize = media.FileSize;
            existingMedia.Category = media.Category;
            existingMedia.AltText = media.AltText;
            existingMedia.IsActive = media.IsActive;
            existingMedia.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Media item updated successfully.";

            return RedirectToAction(nameof(MediaLibrary));
        }

        // GET: Media Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> MediaDetails(int id)
        {
            var media = await _context.Media
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
                return NotFound();

            return View(media);
        }

        // POST: Delete Media Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.Media.FindAsync(id);

            if (media == null)
                return NotFound();

            _context.Media.Remove(media);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Media item deleted successfully.";

            return RedirectToAction(nameof(MediaLibrary));
        }

        // POST: Activate Media Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateMedia(int id)
        {
            var media = await _context.Media.FindAsync(id);

            if (media == null)
                return NotFound();

            media.IsActive = true;
            media.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Media item activated successfully.";

            return RedirectToAction(nameof(MediaLibrary));
        }

        // POST: Deactivate Media Item
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateMedia(int id)
        {
            var media = await _context.Media.FindAsync(id);

            if (media == null)
                return NotFound();

            media.IsActive = false;
            media.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Media item deactivated successfully.";

            return RedirectToAction(nameof(MediaLibrary));
        }

        // =====================================================
        // SERVICES MANAGEMENT
        // =====================================================

        // GET: Services List + Search + Active Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Services(
            string? searchString,
            string? status)
        {
            var services = _context.Services
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                services = services.Where(s =>
                    s.Title.Contains(searchString) ||
                    s.Slug.Contains(searchString) ||
                    (s.ShortDescription != null &&
                     s.ShortDescription.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    services = services.Where(s => s.IsActive);
                else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    services = services.Where(s => !s.IsActive);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            var list = await services
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Service
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateService()
        {
            return View(new Service());
        }

        // POST: Create Service
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(Service service)
        {
            service.Title = service.Title?.Trim() ?? string.Empty;
            service.Slug = service.Slug?.Trim().Trim('/') ?? string.Empty;

            bool slugExists = !string.IsNullOrWhiteSpace(service.Slug)
                && await _context.Services.AnyAsync(s =>
                    s.Slug.ToLower() == service.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(service.Slug),
                    "This service slug already exists.");
            }

            if (service.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(service.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(service);

            service.CreatedAt = DateTime.UtcNow;
            service.UpdatedAt = null;

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Service created successfully.";

            return RedirectToAction(nameof(Services));
        }

        // GET: Edit Service
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // POST: Edit Service
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, Service service)
        {
            if (id != service.Id)
                return BadRequest();

            service.Title = service.Title?.Trim() ?? string.Empty;
            service.Slug = service.Slug?.Trim().Trim('/') ?? string.Empty;

            bool slugExists = !string.IsNullOrWhiteSpace(service.Slug)
                && await _context.Services.AnyAsync(s =>
                    s.Id != id &&
                    s.Slug.ToLower() == service.Slug.ToLower());

            if (slugExists)
            {
                ModelState.AddModelError(
                    nameof(service.Slug),
                    "This service slug already exists.");
            }

            if (service.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(service.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(service);

            var existingService = await _context.Services.FindAsync(id);

            if (existingService == null)
                return NotFound();

            existingService.Title = service.Title;
            existingService.Slug = service.Slug;
            existingService.ShortDescription = service.ShortDescription;
            existingService.Description = service.Description;
            existingService.Icon = service.Icon;
            existingService.ImageUrl = service.ImageUrl;
            existingService.DisplayOrder = service.DisplayOrder;
            existingService.IsActive = service.IsActive;
            existingService.MetaTitle = service.MetaTitle;
            existingService.MetaDescription = service.MetaDescription;
            existingService.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Service updated successfully.";

            return RedirectToAction(nameof(Services));
        }

        // GET: Service Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ServiceDetails(int id)
        {
            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // POST: Delete Service
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Service deleted successfully.";

            return RedirectToAction(nameof(Services));
        }

        // POST: Activate Service
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            service.IsActive = true;
            service.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Service activated successfully.";

            return RedirectToAction(nameof(Services));
        }

        // POST: Deactivate Service
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            service.IsActive = false;
            service.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Service deactivated successfully.";

            return RedirectToAction(nameof(Services));
        }

        // =====================================================
        // PRODUCTS MANAGEMENT
        // =====================================================

        // GET: Products List + Search + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Products(
            string? searchString,
            string? status)
        {
            var products = _context.Products
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(searchString) ||
                    (p.SKU != null && p.SKU.Contains(searchString)) ||
                    (p.ShortDescription != null &&
                     p.ShortDescription.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    products = products.Where(p => p.IsActive);
                else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    products = products.Where(p => !p.IsActive);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            var list = await products
                .OrderBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Product
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateProduct()
        {
            return View(new Product());
        }

        // POST: Create Product
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            product.ProductName = product.ProductName?.Trim() ?? string.Empty;
            product.SKU = string.IsNullOrWhiteSpace(product.SKU)
                ? null
                : product.SKU.Trim();

            if (product.SKU != null)
            {
                bool skuExists = await _context.Products.AnyAsync(p =>
                    p.SKU != null &&
                    p.SKU.ToLower() == product.SKU.ToLower());

                if (skuExists)
                {
                    ModelState.AddModelError(
                        nameof(product.SKU),
                        "This SKU already exists.");
                }
            }

            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError(
                    nameof(product.StockQuantity),
                    "Stock quantity cannot be negative.");
            }

            if (product.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(product.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(product);

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = null;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product created successfully.";

            return RedirectToAction(nameof(Products));
        }

        // GET: Edit Product
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: Edit Product
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product)
        {
            if (id != product.Id)
                return BadRequest();

            product.ProductName = product.ProductName?.Trim() ?? string.Empty;
            product.SKU = string.IsNullOrWhiteSpace(product.SKU)
                ? null
                : product.SKU.Trim();

            if (product.SKU != null)
            {
                bool skuExists = await _context.Products.AnyAsync(p =>
                    p.Id != id &&
                    p.SKU != null &&
                    p.SKU.ToLower() == product.SKU.ToLower());

                if (skuExists)
                {
                    ModelState.AddModelError(
                        nameof(product.SKU),
                        "This SKU already exists.");
                }
            }

            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError(
                    nameof(product.StockQuantity),
                    "Stock quantity cannot be negative.");
            }

            if (product.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(product.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(product);

            var existingProduct = await _context.Products.FindAsync(id);

            if (existingProduct == null)
                return NotFound();

            existingProduct.ProductName = product.ProductName;
            existingProduct.SKU = product.SKU;
            existingProduct.ShortDescription = product.ShortDescription;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.IsActive = product.IsActive;
            existingProduct.DisplayOrder = product.DisplayOrder;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product updated successfully.";

            return RedirectToAction(nameof(Products));
        }

        // GET: Product Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: Delete Product
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product deleted successfully.";

            return RedirectToAction(nameof(Products));
        }

        // POST: Activate Product
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            product.IsActive = true;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product activated successfully.";

            return RedirectToAction(nameof(Products));
        }

        // POST: Deactivate Product
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product deactivated successfully.";

            return RedirectToAction(nameof(Products));
        }

        // =====================================================
        // TESTIMONIALS MANAGEMENT
        // =====================================================

        // GET: Testimonials List + Search + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Testimonials(
            string? searchString,
            string? status)
        {
            var testimonials = _context.Testimonials
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                testimonials = testimonials.Where(t =>
                    t.ClientName.Contains(searchString) ||
                    (t.Designation != null &&
                     t.Designation.Contains(searchString)) ||
                    (t.CompanyName != null &&
                     t.CompanyName.Contains(searchString)) ||
                    t.Content.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    testimonials = testimonials.Where(t => t.IsActive);
                else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    testimonials = testimonials.Where(t => !t.IsActive);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            var list = await testimonials
                .OrderBy(t => t.DisplayOrder)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Testimonial
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateTestimonial()
        {
            return View(new Testimonial());
        }

        // POST: Create Testimonial
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestimonial(Testimonial testimonial)
        {
            testimonial.ClientName = testimonial.ClientName?.Trim() ?? string.Empty;
            testimonial.Content = testimonial.Content?.Trim() ?? string.Empty;

            if (testimonial.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(testimonial.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(testimonial);

            testimonial.CreatedAt = DateTime.UtcNow;
            testimonial.UpdatedAt = null;

            _context.Testimonials.Add(testimonial);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Testimonial created successfully.";

            return RedirectToAction(nameof(Testimonials));
        }

        // GET: Edit Testimonial
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditTestimonial(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);

            if (testimonial == null)
                return NotFound();

            return View(testimonial);
        }

        // POST: Edit Testimonial
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestimonial(
            int id,
            Testimonial testimonial)
        {
            if (id != testimonial.Id)
                return BadRequest();

            testimonial.ClientName = testimonial.ClientName?.Trim() ?? string.Empty;
            testimonial.Content = testimonial.Content?.Trim() ?? string.Empty;

            if (testimonial.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(testimonial.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(testimonial);

            var existingTestimonial = await _context.Testimonials.FindAsync(id);

            if (existingTestimonial == null)
                return NotFound();

            existingTestimonial.ClientName = testimonial.ClientName;
            existingTestimonial.Designation = testimonial.Designation;
            existingTestimonial.CompanyName = testimonial.CompanyName;
            existingTestimonial.Content = testimonial.Content;
            existingTestimonial.ImageUrl = testimonial.ImageUrl;
            existingTestimonial.Rating = testimonial.Rating;
            existingTestimonial.IsActive = testimonial.IsActive;
            existingTestimonial.DisplayOrder = testimonial.DisplayOrder;
            existingTestimonial.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Testimonial updated successfully.";

            return RedirectToAction(nameof(Testimonials));
        }

        // GET: Testimonial Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> TestimonialDetails(int id)
        {
            var testimonial = await _context.Testimonials
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (testimonial == null)
                return NotFound();

            return View(testimonial);
        }

        // POST: Delete Testimonial
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);

            if (testimonial == null)
                return NotFound();

            _context.Testimonials.Remove(testimonial);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Testimonial deleted successfully.";

            return RedirectToAction(nameof(Testimonials));
        }

        // POST: Activate Testimonial
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateTestimonial(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);

            if (testimonial == null)
                return NotFound();

            testimonial.IsActive = true;
            testimonial.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Testimonial activated successfully.";

            return RedirectToAction(nameof(Testimonials));
        }

        // POST: Deactivate Testimonial
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateTestimonial(int id)
        {
            var testimonial = await _context.Testimonials.FindAsync(id);

            if (testimonial == null)
                return NotFound();

            testimonial.IsActive = false;
            testimonial.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Testimonial deactivated successfully.";

            return RedirectToAction(nameof(Testimonials));
        }

        // =====================================================
        // TEAM MEMBERS MANAGEMENT
        // =====================================================

        // GET: Team Members + Search + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> TeamMembers(
            string? searchString,
            string? status)
        {
            var members = _context.TeamMembers
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                members = members.Where(t =>
                    t.Name.Contains(searchString) ||
                    t.Designation.Contains(searchString) ||
                    (t.Email != null && t.Email.Contains(searchString)) ||
                    (t.Description != null &&
                     t.Description.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    members = members.Where(t => t.IsActive);
                else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    members = members.Where(t => !t.IsActive);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            var list = await members
                .OrderBy(t => t.DisplayOrder)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Team Member
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateTeamMember()
        {
            return View(new TeamMember());
        }

        // POST: Create Team Member
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeamMember(TeamMember member)
        {
            member.Name = member.Name?.Trim() ?? string.Empty;
            member.Designation = member.Designation?.Trim() ?? string.Empty;

            if (member.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(member.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(member);

            member.CreatedAt = DateTime.UtcNow;
            member.UpdatedAt = null;

            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Team member created successfully.";

            return RedirectToAction(nameof(TeamMembers));
        }

        // GET: Edit Team Member
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditTeamMember(int id)
        {
            var member = await _context.TeamMembers.FindAsync(id);

            if (member == null)
                return NotFound();

            return View(member);
        }

        // POST: Edit Team Member
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeamMember(
            int id,
            TeamMember member)
        {
            if (id != member.Id)
                return BadRequest();

            member.Name = member.Name?.Trim() ?? string.Empty;
            member.Designation = member.Designation?.Trim() ?? string.Empty;

            if (member.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(member.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(member);

            var existingMember = await _context.TeamMembers.FindAsync(id);

            if (existingMember == null)
                return NotFound();

            existingMember.Name = member.Name;
            existingMember.Designation = member.Designation;
            existingMember.Description = member.Description;
            existingMember.ImageUrl = member.ImageUrl;
            existingMember.Email = member.Email;
            existingMember.LinkedInUrl = member.LinkedInUrl;
            existingMember.TwitterUrl = member.TwitterUrl;
            existingMember.IsActive = member.IsActive;
            existingMember.DisplayOrder = member.DisplayOrder;
            existingMember.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Team member updated successfully.";

            return RedirectToAction(nameof(TeamMembers));
        }

        // GET: Team Member Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> TeamMemberDetails(int id)
        {
            var member = await _context.TeamMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (member == null)
                return NotFound();

            return View(member);
        }

        // POST: Delete Team Member
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeamMember(int id)
        {
            var member = await _context.TeamMembers.FindAsync(id);

            if (member == null)
                return NotFound();

            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Team member deleted successfully.";

            return RedirectToAction(nameof(TeamMembers));
        }

        // POST: Activate Team Member
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateTeamMember(int id)
        {
            var member = await _context.TeamMembers.FindAsync(id);

            if (member == null)
                return NotFound();

            member.IsActive = true;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Team member activated successfully.";

            return RedirectToAction(nameof(TeamMembers));
        }

        // POST: Deactivate Team Member
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateTeamMember(int id)
        {
            var member = await _context.TeamMembers.FindAsync(id);

            if (member == null)
                return NotFound();

            member.IsActive = false;
            member.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Team member deactivated successfully.";

            return RedirectToAction(nameof(TeamMembers));
        }

        // =====================================================
        // FAQ MANAGEMENT
        // =====================================================

        // GET: FAQ List + Search + Category + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> FAQs(
            string? searchString,
            string? category,
            string? status)
        {
            var faqs = _context.FAQs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                faqs = faqs.Where(f =>
                    f.Question.Contains(searchString) ||
                    f.Answer.Contains(searchString) ||
                    (f.Category != null &&
                     f.Category.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                faqs = faqs.Where(f => f.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    faqs = faqs.Where(f => f.IsActive);
                else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    faqs = faqs.Where(f => !f.IsActive);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Category = category;
            ViewBag.Status = status;

            ViewBag.Categories = await _context.FAQs
                .AsNoTracking()
                .Where(f => f.Category != null && f.Category != "")
                .Select(f => f.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var list = await faqs
                .OrderBy(f => f.DisplayOrder)
                .ThenByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create FAQ
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateFAQ()
        {
            return View(new FAQ());
        }

        // POST: Create FAQ
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFAQ(FAQ faq)
        {
            faq.Question = faq.Question?.Trim() ?? string.Empty;
            faq.Answer = faq.Answer?.Trim() ?? string.Empty;
            faq.Category = string.IsNullOrWhiteSpace(faq.Category)
                ? null
                : faq.Category.Trim();

            if (faq.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(faq.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(faq);

            faq.CreatedAt = DateTime.UtcNow;
            faq.UpdatedAt = null;

            _context.FAQs.Add(faq);
            await _context.SaveChangesAsync();

            TempData["Success"] = "FAQ created successfully.";

            return RedirectToAction(nameof(FAQs));
        }

        // GET: Edit FAQ
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditFAQ(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);

            if (faq == null)
                return NotFound();

            return View(faq);
        }

        // POST: Edit FAQ
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFAQ(int id, FAQ faq)
        {
            if (id != faq.Id)
                return BadRequest();

            faq.Question = faq.Question?.Trim() ?? string.Empty;
            faq.Answer = faq.Answer?.Trim() ?? string.Empty;
            faq.Category = string.IsNullOrWhiteSpace(faq.Category)
                ? null
                : faq.Category.Trim();

            if (faq.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(faq.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(faq);

            var existingFAQ = await _context.FAQs.FindAsync(id);

            if (existingFAQ == null)
                return NotFound();

            existingFAQ.Question = faq.Question;
            existingFAQ.Answer = faq.Answer;
            existingFAQ.Category = faq.Category;
            existingFAQ.IsActive = faq.IsActive;
            existingFAQ.DisplayOrder = faq.DisplayOrder;
            existingFAQ.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "FAQ updated successfully.";

            return RedirectToAction(nameof(FAQs));
        }

        // GET: FAQ Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> FAQDetails(int id)
        {
            var faq = await _context.FAQs
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (faq == null)
                return NotFound();

            return View(faq);
        }

        // POST: Delete FAQ
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFAQ(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);

            if (faq == null)
                return NotFound();

            _context.FAQs.Remove(faq);
            await _context.SaveChangesAsync();

            TempData["Success"] = "FAQ deleted successfully.";

            return RedirectToAction(nameof(FAQs));
        }

        // POST: Activate FAQ
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateFAQ(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);

            if (faq == null)
                return NotFound();

            faq.IsActive = true;
            faq.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "FAQ activated successfully.";

            return RedirectToAction(nameof(FAQs));
        }

        // POST: Deactivate FAQ
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateFAQ(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);

            if (faq == null)
                return NotFound();

            faq.IsActive = false;
            faq.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "FAQ deactivated successfully.";

            return RedirectToAction(nameof(FAQs));
        }

        // =====================================================
        // ANNOUNCEMENTS MANAGEMENT
        // =====================================================

        // GET: Announcements + Search + Type + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Announcements(
            string? searchString,
            string? type,
            string? status)
        {
            var announcements = _context.Announcements
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                announcements = announcements.Where(a =>
                    a.Title.Contains(searchString) ||
                    a.Description.Contains(searchString) ||
                    (a.Type != null && a.Type.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                announcements = announcements.Where(a => a.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    announcements = announcements.Where(a => a.IsActive);
                else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    announcements = announcements.Where(a => !a.IsActive);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Type = type;
            ViewBag.Status = status;

            ViewBag.Types = await _context.Announcements
                .AsNoTracking()
                .Where(a => a.Type != null && a.Type != "")
                .Select(a => a.Type!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            var list = await announcements
                .OrderBy(a => a.DisplayOrder)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Announcement
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateAnnouncement()
        {
            return View(new Announcement());
        }

        // POST: Create Announcement
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(
            Announcement announcement)
        {
            announcement.Title = announcement.Title?.Trim() ?? string.Empty;
            announcement.Description =
                announcement.Description?.Trim() ?? string.Empty;

            announcement.Type = string.IsNullOrWhiteSpace(announcement.Type)
                ? null
                : announcement.Type.Trim();

            if (announcement.EndDate.HasValue &&
                announcement.EndDate.Value.Date < announcement.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(announcement.EndDate),
                    "End date cannot be earlier than start date.");
            }

            if (announcement.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(announcement.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(announcement);

            announcement.CreatedAt = DateTime.UtcNow;
            announcement.UpdatedAt = null;

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement created successfully.";

            return RedirectToAction(nameof(Announcements));
        }

        // GET: Edit Announcement
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);

            if (announcement == null)
                return NotFound();

            return View(announcement);
        }

        // POST: Edit Announcement
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAnnouncement(
            int id,
            Announcement announcement)
        {
            if (id != announcement.Id)
                return BadRequest();

            announcement.Title = announcement.Title?.Trim() ?? string.Empty;
            announcement.Description =
                announcement.Description?.Trim() ?? string.Empty;

            announcement.Type = string.IsNullOrWhiteSpace(announcement.Type)
                ? null
                : announcement.Type.Trim();

            if (announcement.EndDate.HasValue &&
                announcement.EndDate.Value.Date < announcement.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(announcement.EndDate),
                    "End date cannot be earlier than start date.");
            }

            if (announcement.DisplayOrder < 0)
            {
                ModelState.AddModelError(
                    nameof(announcement.DisplayOrder),
                    "Display order cannot be negative.");
            }

            if (!ModelState.IsValid)
                return View(announcement);

            var existingAnnouncement =
                await _context.Announcements.FindAsync(id);

            if (existingAnnouncement == null)
                return NotFound();

            existingAnnouncement.Title = announcement.Title;
            existingAnnouncement.Description = announcement.Description;
            existingAnnouncement.Type = announcement.Type;
            existingAnnouncement.StartDate = announcement.StartDate;
            existingAnnouncement.EndDate = announcement.EndDate;
            existingAnnouncement.IsActive = announcement.IsActive;
            existingAnnouncement.DisplayOrder = announcement.DisplayOrder;
            existingAnnouncement.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement updated successfully.";

            return RedirectToAction(nameof(Announcements));
        }

        // GET: Announcement Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AnnouncementDetails(int id)
        {
            var announcement = await _context.Announcements
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null)
                return NotFound();

            return View(announcement);
        }

        // POST: Delete Announcement
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);

            if (announcement == null)
                return NotFound();

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement deleted successfully.";

            return RedirectToAction(nameof(Announcements));
        }

        // POST: Activate Announcement
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);

            if (announcement == null)
                return NotFound();

            announcement.IsActive = true;
            announcement.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement activated successfully.";

            return RedirectToAction(nameof(Announcements));
        }

        // POST: Deactivate Announcement
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);

            if (announcement == null)
                return NotFound();

            announcement.IsActive = false;
            announcement.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement deactivated successfully.";

            return RedirectToAction(nameof(Announcements));
        }

        // =====================================================
        // EVENTS MANAGEMENT
        // =====================================================

        // GET: Events + Search + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Events(
            string? searchString,
            string? status)
        {
            var eventsQuery = _context.Events
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Title.Contains(searchString) ||
                    e.Location.Contains(searchString) ||
                    e.Description.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                eventsQuery = eventsQuery.Where(e => e.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            ViewBag.Statuses = await _context.Events
                .AsNoTracking()
                .Where(e => e.Status != null && e.Status != "")
                .Select(e => e.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var list = await eventsQuery
                .OrderByDescending(e => e.StartDate)
                .ThenByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Event
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new Event());
        }

        // POST: Create Event
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event eventItem)
        {
            eventItem.Title = eventItem.Title?.Trim() ?? string.Empty;
            eventItem.Location = eventItem.Location?.Trim() ?? string.Empty;
            eventItem.Description = eventItem.Description?.Trim() ?? string.Empty;
            eventItem.Status = string.IsNullOrWhiteSpace(eventItem.Status)
                ? "Draft"
                : eventItem.Status.Trim();

            if (eventItem.EndDate.HasValue &&
                eventItem.EndDate.Value.Date < eventItem.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(eventItem.EndDate),
                    "End date cannot be earlier than start date.");
            }

            if (!ModelState.IsValid)
                return View(eventItem);

            eventItem.CreatedAt = DateTime.UtcNow;
            eventItem.UpdatedAt = null;

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Event created successfully.";

            return RedirectToAction(nameof(Events));
        }

        // GET: Edit Event
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
                return NotFound();

            return View(eventItem);
        }

        // POST: Edit Event
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, Event eventItem)
        {
            if (id != eventItem.Id)
                return BadRequest();

            eventItem.Title = eventItem.Title?.Trim() ?? string.Empty;
            eventItem.Location = eventItem.Location?.Trim() ?? string.Empty;
            eventItem.Description = eventItem.Description?.Trim() ?? string.Empty;
            eventItem.Status = eventItem.Status?.Trim() ?? "Draft";

            if (string.IsNullOrWhiteSpace(eventItem.Status))
                eventItem.Status = "Draft";

            if (eventItem.EndDate.HasValue &&
                eventItem.EndDate.Value.Date < eventItem.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(eventItem.EndDate),
                    "End date cannot be earlier than start date.");
            }

            if (!ModelState.IsValid)
                return View(eventItem);

            var existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
                return NotFound();

            existingEvent.Title = eventItem.Title;
            existingEvent.Location = eventItem.Location;
            existingEvent.Description = eventItem.Description;
            existingEvent.Image = eventItem.Image;
            existingEvent.StartDate = eventItem.StartDate;
            existingEvent.EndDate = eventItem.EndDate;
            existingEvent.RegistrationLink = eventItem.RegistrationLink;
            existingEvent.Status = eventItem.Status;
            existingEvent.MetaTitle = eventItem.MetaTitle;
            existingEvent.MetaDescription = eventItem.MetaDescription;
            existingEvent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Event updated successfully.";

            return RedirectToAction(nameof(Events));
        }

        // GET: Event Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EventDetails(int id)
        {
            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
                return NotFound();

            return View(eventItem);
        }

        // POST: Delete Event
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
                return NotFound();

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Event deleted successfully.";

            return RedirectToAction(nameof(Events));
        }

        // POST: Publish Event
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
                return NotFound();

            eventItem.Status = "Published";
            eventItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Event published successfully.";

            return RedirectToAction(nameof(Events));
        }

        // POST: Unpublish Event
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpublishEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
                return NotFound();

            eventItem.Status = "Draft";
            eventItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Event unpublished successfully.";

            return RedirectToAction(nameof(Events));
        }

        // =====================================================
        // NEWS MANAGEMENT
        // =====================================================

        // GET: News + Search + Category + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> News(
            string? searchString,
            string? category,
            string? status)
        {
            var newsQuery = _context.News
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                newsQuery = newsQuery.Where(n =>
                    n.Title.Contains(searchString) ||
                    n.Slug.Contains(searchString) ||
                    n.Author.Contains(searchString) ||
                    n.Excerpt.Contains(searchString) ||
                    n.Content.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(category))
                newsQuery = newsQuery.Where(n => n.Category == category);

            if (!string.IsNullOrWhiteSpace(status))
                newsQuery = newsQuery.Where(n => n.Status == status);

            ViewBag.SearchString = searchString;
            ViewBag.Category = category;
            ViewBag.Status = status;

            ViewBag.Categories = await _context.News
                .AsNoTracking()
                .Where(n => n.Category != null && n.Category != "")
                .Select(n => n.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Statuses = await _context.News
                .AsNoTracking()
                .Where(n => n.Status != null && n.Status != "")
                .Select(n => n.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var list = await newsQuery
                .OrderByDescending(n => n.PublicationDate)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create News
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateNews()
        {
            return View(new News());
        }

        // POST: Create News
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNews(News news)
        {
            news.Title = news.Title?.Trim() ?? string.Empty;
            news.Slug = news.Slug?.Trim().Trim('/') ?? string.Empty;
            news.Author = news.Author?.Trim() ?? string.Empty;
            news.Excerpt = news.Excerpt?.Trim() ?? string.Empty;
            news.Content = news.Content?.Trim() ?? string.Empty;
            news.Image = news.Image?.Trim() ?? string.Empty;
            news.Attachment = news.Attachment?.Trim() ?? string.Empty;
            news.Category = news.Category?.Trim() ?? string.Empty;
            news.Status = news.Status?.Trim() ?? "Draft";
            news.MetaTitle = news.MetaTitle?.Trim() ?? string.Empty;
            news.MetaDescription = news.MetaDescription?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(news.Slug))
            {
                bool slugExists = await _context.News.AnyAsync(n =>
                    n.Slug.ToLower() == news.Slug.ToLower());

                if (slugExists)
                {
                    ModelState.AddModelError(
                        nameof(news.Slug),
                        "This news slug already exists.");
                }
            }

            if (!ModelState.IsValid)
                return View(news);

            news.CreatedAt = DateTime.Now;
            news.UpdatedAt = null;

            _context.News.Add(news);
            await _context.SaveChangesAsync();

            TempData["Success"] = "News created successfully.";

            return RedirectToAction(nameof(News));
        }

        // GET: Edit News
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditNews(int id)
        {
            var news = await _context.News.FindAsync(id);

            if (news == null)
                return NotFound();

            return View(news);
        }

        // POST: Edit News
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNews(int id, News news)
        {
            if (id != news.Id)
                return BadRequest();

            news.Title = news.Title?.Trim() ?? string.Empty;
            news.Slug = news.Slug?.Trim().Trim('/') ?? string.Empty;
            news.Author = news.Author?.Trim() ?? string.Empty;
            news.Excerpt = news.Excerpt?.Trim() ?? string.Empty;
            news.Content = news.Content?.Trim() ?? string.Empty;
            news.Image = news.Image?.Trim() ?? string.Empty;
            news.Attachment = news.Attachment?.Trim() ?? string.Empty;
            news.Category = news.Category?.Trim() ?? string.Empty;
            news.Status = news.Status?.Trim() ?? "Draft";
            news.MetaTitle = news.MetaTitle?.Trim() ?? string.Empty;
            news.MetaDescription = news.MetaDescription?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(news.Slug))
            {
                bool slugExists = await _context.News.AnyAsync(n =>
                    n.Id != id &&
                    n.Slug.ToLower() == news.Slug.ToLower());

                if (slugExists)
                {
                    ModelState.AddModelError(
                        nameof(news.Slug),
                        "This news slug already exists.");
                }
            }

            if (!ModelState.IsValid)
                return View(news);

            var existingNews = await _context.News.FindAsync(id);

            if (existingNews == null)
                return NotFound();

            existingNews.Title = news.Title;
            existingNews.Slug = news.Slug;
            existingNews.Content = news.Content;
            existingNews.Excerpt = news.Excerpt;
            existingNews.Author = news.Author;
            existingNews.PublicationDate = news.PublicationDate;
            existingNews.Image = news.Image;
            existingNews.Attachment = news.Attachment;
            existingNews.Category = news.Category;
            existingNews.Status = news.Status;
            existingNews.MetaTitle = news.MetaTitle;
            existingNews.MetaDescription = news.MetaDescription;
            existingNews.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "News updated successfully.";

            return RedirectToAction(nameof(News));
        }

        // GET: News Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> NewsDetails(int id)
        {
            var news = await _context.News
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null)
                return NotFound();

            return View(news);
        }

        // POST: Delete News
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var news = await _context.News.FindAsync(id);

            if (news == null)
                return NotFound();

            _context.News.Remove(news);
            await _context.SaveChangesAsync();

            TempData["Success"] = "News deleted successfully.";

            return RedirectToAction(nameof(News));
        }

        // POST: Publish News
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishNews(int id)
        {
            var news = await _context.News.FindAsync(id);

            if (news == null)
                return NotFound();

            news.Status = "Published";
            news.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "News published successfully.";

            return RedirectToAction(nameof(News));
        }

        // POST: Unpublish News
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpublishNews(int id)
        {
            var news = await _context.News.FindAsync(id);

            if (news == null)
                return NotFound();

            news.Status = "Draft";
            news.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "News unpublished successfully.";

            return RedirectToAction(nameof(News));
        }

        // =====================================================
        // DOWNLOADS MANAGEMENT
        // =====================================================

        // GET: Downloads + Search + Category + Status Filter
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Downloads(
            string? searchString,
            string? category,
            string? status)
        {
            var downloads = _context.Downloads
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                downloads = downloads.Where(d =>
                    d.Title.Contains(searchString) ||
                    d.Description.Contains(searchString) ||
                    d.FileName.Contains(searchString) ||
                    d.Category.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(category))
                downloads = downloads.Where(d => d.Category == category);

            if (!string.IsNullOrWhiteSpace(status))
                downloads = downloads.Where(d => d.Status == status);

            ViewBag.SearchString = searchString;
            ViewBag.Category = category;
            ViewBag.Status = status;

            ViewBag.Categories = await _context.Downloads
                .AsNoTracking()
                .Select(d => d.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Statuses = await _context.Downloads
                .AsNoTracking()
                .Select(d => d.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var list = await downloads
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: Create Download
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateDownload()
        {
            return View(new Download());
        }

        // POST: Create Download
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDownload(Download download)
        {
            download.Title = download.Title?.Trim() ?? string.Empty;
            download.Description = download.Description?.Trim() ?? string.Empty;
            download.Category = download.Category?.Trim() ?? string.Empty;
            download.FilePath = download.FilePath?.Trim() ?? string.Empty;
            download.FileName = download.FileName?.Trim() ?? string.Empty;
            download.Status = download.Status?.Trim() ?? "Active";

            if (!ModelState.IsValid)
                return View(download);

            download.CreatedAt = DateTime.UtcNow;
            download.UpdatedAt = null;

            _context.Downloads.Add(download);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Download created successfully.";

            return RedirectToAction(nameof(Downloads));
        }

        // GET: Edit Download
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditDownload(int id)
        {
            var download = await _context.Downloads.FindAsync(id);

            if (download == null)
                return NotFound();

            return View(download);
        }

        // POST: Edit Download
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDownload(int id, Download download)
        {
            if (id != download.Id)
                return BadRequest();

            download.Title = download.Title?.Trim() ?? string.Empty;
            download.Description = download.Description?.Trim() ?? string.Empty;
            download.Category = download.Category?.Trim() ?? string.Empty;
            download.FilePath = download.FilePath?.Trim() ?? string.Empty;
            download.FileName = download.FileName?.Trim() ?? string.Empty;
            download.Status = download.Status?.Trim() ?? "Active";

            if (!ModelState.IsValid)
                return View(download);

            var existingDownload = await _context.Downloads.FindAsync(id);

            if (existingDownload == null)
                return NotFound();

            existingDownload.Title = download.Title;
            existingDownload.Description = download.Description;
            existingDownload.Category = download.Category;
            existingDownload.FilePath = download.FilePath;
            existingDownload.FileName = download.FileName;
            existingDownload.Status = download.Status;
            existingDownload.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Download updated successfully.";

            return RedirectToAction(nameof(Downloads));
        }

        // GET: Download Details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> DownloadDetails(int id)
        {
            var download = await _context.Downloads
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (download == null)
                return NotFound();

            return View(download);
        }

        // POST: Delete Download
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDownload(int id)
        {
            var download = await _context.Downloads.FindAsync(id);

            if (download == null)
                return NotFound();

            _context.Downloads.Remove(download);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Download deleted successfully.";

            return RedirectToAction(nameof(Downloads));
        }

        // POST: Activate Download
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateDownload(int id)
        {
            var download = await _context.Downloads.FindAsync(id);

            if (download == null)
                return NotFound();

            download.Status = "Active";
            download.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Download activated successfully.";

            return RedirectToAction(nameof(Downloads));
        }

        // POST: Deactivate Download
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateDownload(int id)
        {
            var download = await _context.Downloads.FindAsync(id);

            if (download == null)
                return NotFound();

            download.Status = "Inactive";
            download.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Download deactivated successfully.";

            return RedirectToAction(nameof(Downloads));
        }

        // =====================================================
        // CAREER MANAGEMENT
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Careers(string searchString, string status)
        {
            var careers = _context.Careers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                careers = careers.Where(c =>
                    c.JobTitle.Contains(searchString) ||
                    c.Department.Contains(searchString) ||
                    c.Location.Contains(searchString) ||
                    c.JobType.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                careers = careers.Where(c => c.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.StatusList = await _context.Careers
                .AsNoTracking()
                .Select(c => c.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var result = await careers
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(result);
        }

        // CREATE CAREER - GET
        [HttpGet]
        public IActionResult CreateCareer()
        {
            return View();
        }

        // CREATE CAREER - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCareer(Career career)
        {
            if (career.LastDate.HasValue &&
                career.LastDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(career.LastDate),
                    "Last date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                career.CreatedAt = DateTime.Now;
                career.UpdatedAt = null;
                career.IsPublished = false;
                career.Status = "Draft";

                _context.Careers.Add(career);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Career created successfully.";
                return RedirectToAction(nameof(Careers));
            }

            return View(career);
        }

        // EDIT CAREER - GET
        [HttpGet]
        public async Task<IActionResult> EditCareer(int id)
        {
            var career = await _context.Careers.FindAsync(id);

            if (career == null)
                return NotFound();

            return View(career);
        }

        // EDIT CAREER - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCareer(int id, Career career)
        {
            if (id != career.Id)
                return BadRequest();

            if (career.LastDate.HasValue &&
                career.LastDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(career.LastDate),
                    "Last date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                var existingCareer = await _context.Careers.FindAsync(id);

                if (existingCareer == null)
                    return NotFound();

                existingCareer.JobTitle = career.JobTitle;
                existingCareer.Department = career.Department;
                existingCareer.Location = career.Location;
                existingCareer.JobType = career.JobType;
                existingCareer.Description = career.Description;
                existingCareer.Requirements = career.Requirements;
                existingCareer.Responsibilities = career.Responsibilities;
                existingCareer.Experience = career.Experience;
                existingCareer.Salary = career.Salary;
                existingCareer.LastDate = career.LastDate;
                existingCareer.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Career updated successfully.";
                return RedirectToAction(nameof(Careers));
            }

            return View(career);
        }

        // CAREER DETAILS
        [HttpGet]
        public async Task<IActionResult> CareerDetails(int id)
        {
            var career = await _context.Careers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (career == null)
                return NotFound();

            return View(career);
        }

        // DELETE CAREER - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCareer(int id)
        {
            var career = await _context.Careers.FindAsync(id);

            if (career == null)
            {
                TempData["Error"] = "Career not found.";
                return RedirectToAction(nameof(Careers));
            }

            _context.Careers.Remove(career);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Career deleted successfully.";
            return RedirectToAction(nameof(Careers));
        }

        // PUBLISH CAREER - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishCareer(int id)
        {
            var career = await _context.Careers.FindAsync(id);

            if (career == null)
                return NotFound();

            career.IsPublished = true;
            career.Status = "Published";
            career.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Career published successfully.";
            return RedirectToAction(nameof(Careers));
        }

        // UNPUBLISH CAREER - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpublishCareer(int id)
        {
            var career = await _context.Careers.FindAsync(id);

            if (career == null)
                return NotFound();

            career.IsPublished = false;
            career.Status = "Draft";
            career.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Career unpublished successfully.";
            return RedirectToAction(nameof(Careers));
        }

        // =====================================================
        // CAREER APPLICATIONS MANAGEMENT
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> CareerApplications(
            string searchString,
            string status)
        {
            var applications = _context.CareerApplications
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                applications = applications.Where(a =>
                    a.ApplicantName.Contains(searchString) ||
                    a.Email.Contains(searchString) ||
                    (a.Phone != null && a.Phone.Contains(searchString)) ||
                    (a.CareerTitle != null && a.CareerTitle.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                applications = applications.Where(a => a.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            ViewBag.StatusList = await _context.CareerApplications
                .AsNoTracking()
                .Select(a => a.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var result = await applications
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(result);
        }

        // APPLICATION DETAILS
        [HttpGet]
        public async Task<IActionResult> CareerApplicationDetails(int id)
        {
            var application = await _context.CareerApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound();

            return View(application);
        }

        // UPDATE APPLICATION STATUS AND NOTES - GET
        [HttpGet]
        public async Task<IActionResult> EditCareerApplication(int id)
        {
            var application = await _context.CareerApplications.FindAsync(id);

            if (application == null)
                return NotFound();

            ViewBag.StatusList = new List<string>
    {
        "Pending",
        "Reviewed",
        "Shortlisted",
        "Rejected",
        "Accepted"
    };

            return View(application);
        }

        // UPDATE APPLICATION STATUS AND NOTES - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCareerApplication(
            int id,
            string status,
            string? notes)
        {
            var allowedStatuses = new[]
            {
        "Pending",
        "Reviewed",
        "Shortlisted",
        "Rejected",
        "Accepted"
    };

            if (!allowedStatuses.Contains(status))
            {
                ModelState.AddModelError(
                    nameof(status),
                    "Please select a valid application status.");

                var invalidApplication =
                    await _context.CareerApplications.FindAsync(id);

                if (invalidApplication == null)
                    return NotFound();

                ViewBag.StatusList = allowedStatuses.ToList();
                return View(invalidApplication);
            }

            var application = await _context.CareerApplications.FindAsync(id);

            if (application == null)
                return NotFound();

            application.Status = status;
            application.Notes = notes;
            application.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Application updated successfully.";
            return RedirectToAction(nameof(CareerApplications));
        }

        // DELETE APPLICATION - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCareerApplication(int id)
        {
            var application = await _context.CareerApplications.FindAsync(id);

            if (application == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction(nameof(CareerApplications));
            }

            _context.CareerApplications.Remove(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application deleted successfully.";
            return RedirectToAction(nameof(CareerApplications));
        }

        // DELETE APPLICATION - GET
        [HttpGet]
        public async Task<IActionResult> ConfirmDeleteCareerApplication(int id)
        {
            var application = await _context.CareerApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction(nameof(CareerApplications));
            }

            return View("DeleteCareerApplication", application);
        }
        // =====================================================
        // CONTACT SUBMISSIONS MANAGEMENT
        // =====================================================

        // LIST + SEARCH + STATUS FILTER
        [HttpGet]
        public async Task<IActionResult> ContactSubmissions(
            string searchString,
            string status)
        {
            var submissions = _context.ContactSubmissions
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                submissions = submissions.Where(c =>
                    c.Name.Contains(searchString) ||
                    c.Email.Contains(searchString) ||
                    (c.Subject != null && c.Subject.Contains(searchString)) ||
                    c.Message.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                submissions = submissions.Where(c => c.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;

            ViewBag.StatusList = new List<string>
    {
        "New",
        "Read",
        "Replied"
    };

            var result = await submissions
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();

            return View(result);
        }

        // SUBMISSION DETAILS
        [HttpGet]
        public async Task<IActionResult> ContactSubmissionDetails(int id)
        {
            var submission = await _context.ContactSubmissions
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (submission == null)
                return NotFound();

            // New submission ko automatically Read mark karna
            if (submission.Status == "New")
            {
                var existing = await _context.ContactSubmissions
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (existing != null)
                {
                    existing.Status = "Read";
                    existing.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    submission.Status = "Read";
                }
            }

            return View(submission);
        }

        // EDIT SUBMISSION - GET
        [HttpGet]
        public async Task<IActionResult> EditContactSubmission(int id)
        {
            var submission = await _context.ContactSubmissions.FindAsync(id);

            if (submission == null)
                return NotFound();

            ViewBag.StatusList = new List<string>
    {
        "New",
        "Read",
        "Replied"
    };

            return View(submission);
        }

        // EDIT SUBMISSION - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditContactSubmission(
            int id,
            string status,
            string? remarks)
        {
            var allowedStatuses = new[] { "New", "Read", "Replied" };

            if (!allowedStatuses.Contains(status))
            {
                ModelState.AddModelError(
                    nameof(status),
                    "Please select a valid status.");

                var invalidSubmission =
                    await _context.ContactSubmissions.FindAsync(id);

                if (invalidSubmission == null)
                    return NotFound();

                ViewBag.StatusList = allowedStatuses.ToList();
                return View(invalidSubmission);
            }

            var submission = await _context.ContactSubmissions.FindAsync(id);

            if (submission == null)
                return NotFound();

            submission.Status = status;
            submission.Remarks = remarks;
            submission.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Contact submission updated successfully.";
            return RedirectToAction(nameof(ContactSubmissions));
        }

        // DELETE SUBMISSION - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContactSubmission(int id)
        {
            var submission = await _context.ContactSubmissions.FindAsync(id);

            if (submission == null)
            {
                TempData["Error"] = "Contact submission not found.";
                return RedirectToAction(nameof(ContactSubmissions));
            }

            _context.ContactSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Contact submission deleted successfully.";
            return RedirectToAction(nameof(ContactSubmissions));
        }

        // =====================================================
        // FORMS MANAGEMENT
        // =====================================================

        // LIST + SEARCH + STATUS FILTER
        [HttpGet]
        public async Task<IActionResult> Forms(string searchString, string status)
        {
            var forms = _context.Forms
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                forms = forms.Where(f =>
                    f.Name.Contains(searchString) ||
                    f.Slug.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                forms = forms.Where(f => f.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.StatusList = new List<string> { "Active", "Inactive" };

            var result = await forms
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(result);
        }

        // CREATE FORM - GET
        [HttpGet]
        public IActionResult CreateForm()
        {
            return View();
        }

        // CREATE FORM - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateForm(Form form)
        {
            if (!string.IsNullOrWhiteSpace(form.Slug))
            {
                form.Slug = form.Slug.Trim();

                bool slugExists = await _context.Forms
                    .AnyAsync(f => f.Slug == form.Slug);

                if (slugExists)
                {
                    ModelState.AddModelError(
                        nameof(form.Slug),
                        "This slug already exists.");
                }
            }

            if (ModelState.IsValid)
            {
                form.Name = form.Name.Trim();
                form.CreatedAt = DateTime.Now;
                form.UpdatedAt = null;

                _context.Forms.Add(form);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Form created successfully.";
                return RedirectToAction(nameof(Forms));
            }

            return View(form);
        }

        // EDIT FORM - GET
        [HttpGet]
        public async Task<IActionResult> EditForm(int id)
        {
            var form = await _context.Forms.FindAsync(id);

            if (form == null)
                return NotFound();

            return View(form);
        }

        // EDIT FORM - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditForm(int id, Form form)
        {
            if (id != form.Id)
                return BadRequest();

            if (!string.IsNullOrWhiteSpace(form.Slug))
            {
                form.Slug = form.Slug.Trim();

                bool slugExists = await _context.Forms
                    .AnyAsync(f => f.Slug == form.Slug && f.Id != id);

                if (slugExists)
                {
                    ModelState.AddModelError(
                        nameof(form.Slug),
                        "This slug already exists.");
                }
            }

            if (ModelState.IsValid)
            {
                var existingForm = await _context.Forms.FindAsync(id);

                if (existingForm == null)
                    return NotFound();

                existingForm.Name = form.Name.Trim();
                existingForm.Slug = form.Slug;
                existingForm.Description = form.Description;
                existingForm.Status = form.Status;
                existingForm.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Form updated successfully.";
                return RedirectToAction(nameof(Forms));
            }

            return View(form);
        }

        // FORM DETAILS
        [HttpGet]
        public async Task<IActionResult> FormDetails(int id)
        {
            var form = await _context.Forms
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null)
                return NotFound();

            return View(form);
        }

        // DELETE FORM - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForm(int id)
        {
            var form = await _context.Forms.FindAsync(id);

            if (form == null)
            {
                TempData["Error"] = "Form not found.";
                return RedirectToAction(nameof(Forms));
            }

            _context.Forms.Remove(form);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Form deleted successfully.";
            return RedirectToAction(nameof(Forms));
        }

        // ACTIVATE FORM - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateForm(int id)
        {
            var form = await _context.Forms.FindAsync(id);

            if (form == null)
                return NotFound();

            form.Status = "Active";
            form.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Form activated successfully.";
            return RedirectToAction(nameof(Forms));
        }

        // DEACTIVATE FORM - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateForm(int id)
        {
            var form = await _context.Forms.FindAsync(id);

            if (form == null)
                return NotFound();

            form.Status = "Inactive";
            form.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Form deactivated successfully.";
            return RedirectToAction(nameof(Forms));
        }

        // =====================================================
        // NOTIFICATIONS MANAGEMENT
        // =====================================================

        // LIST + SEARCH + FILTER
        [HttpGet]
        public async Task<IActionResult> Notifications(
            string searchString,
            string type,
            string status)
        {
            var notifications = _context.Notifications
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                notifications = notifications.Where(n =>
                    n.Title.Contains(searchString) ||
                    n.Message.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                notifications = notifications.Where(n => n.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                notifications = notifications.Where(n => n.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Type = type;
            ViewBag.Status = status;

            ViewBag.TypeList = new List<string>
    {
        "Info",
        "Success",
        "Warning",
        "Error"
    };

            ViewBag.StatusList = new List<string>
    {
        "Unread",
        "Read"
    };

            var result = await notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(result);
        }

        // CREATE NOTIFICATION - GET
        [HttpGet]
        public IActionResult CreateNotification()
        {
            return View();
        }

        // CREATE NOTIFICATION - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNotification(Notification notification)
        {
            if (ModelState.IsValid)
            {
                notification.CreatedAt = DateTime.Now;
                notification.UpdatedAt = null;

                if (notification.Status == "Read")
                    notification.ReadAt = DateTime.Now;
                else
                {
                    notification.Status = "Unread";
                    notification.ReadAt = null;
                }

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Notification created successfully.";
                return RedirectToAction(nameof(Notifications));
            }

            return View(notification);
        }

        // EDIT NOTIFICATION - GET
        [HttpGet]
        public async Task<IActionResult> EditNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound();

            return View(notification);
        }

        // EDIT NOTIFICATION - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNotification(
            int id,
            Notification notification)
        {
            if (id != notification.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                var existing = await _context.Notifications.FindAsync(id);

                if (existing == null)
                    return NotFound();

                existing.Title = notification.Title;
                existing.Message = notification.Message;
                existing.Type = notification.Type;
                existing.Link = notification.Link;
                existing.UpdatedAt = DateTime.Now;

                if (notification.Status == "Read")
                {
                    existing.Status = "Read";
                    existing.ReadAt ??= DateTime.Now;
                }
                else if (notification.Status == "Unread")
                {
                    existing.Status = "Unread";
                    existing.ReadAt = null;
                }
                else
                {
                    ModelState.AddModelError(
                        nameof(notification.Status),
                        "Please select a valid status.");

                    return View(notification);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Notification updated successfully.";
                return RedirectToAction(nameof(Notifications));
            }

            return View(notification);
        }

        // NOTIFICATION DETAILS
        [HttpGet]
        public async Task<IActionResult> NotificationDetails(int id)
        {
            var notification = await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return NotFound();

            return View(notification);
        }

        // DELETE NOTIFICATION - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                TempData["Error"] = "Notification not found.";
                return RedirectToAction(nameof(Notifications));
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification deleted successfully.";
            return RedirectToAction(nameof(Notifications));
        }

        // MARK AS READ - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound();

            notification.Status = "Read";
            notification.ReadAt ??= DateTime.Now;
            notification.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification marked as read.";
            return RedirectToAction(nameof(Notifications));
        }

        // MARK AS UNREAD - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsUnread(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound();

            notification.Status = "Unread";
            notification.ReadAt = null;
            notification.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification marked as unread.";
            return RedirectToAction(nameof(Notifications));
        }

        // =====================================================
        // REDIRECTS MANAGEMENT
        // =====================================================

        // LIST + SEARCH + FILTER
        [HttpGet]
        public async Task<IActionResult> Redirects(
            string searchString,
            string redirectType,
            string status)
        {
            var redirects = _context.Redirects
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                redirects = redirects.Where(r =>
                    r.SourceUrl.Contains(searchString) ||
                    r.DestinationUrl.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(redirectType))
            {
                redirects = redirects.Where(r =>
                    r.RedirectType == redirectType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Active")
                    redirects = redirects.Where(r => r.IsActive);
                else if (status == "Inactive")
                    redirects = redirects.Where(r => !r.IsActive);
            }

            ViewBag.SearchString = searchString;
            ViewBag.RedirectType = redirectType;
            ViewBag.Status = status;

            ViewBag.RedirectTypes = new List<string> { "301", "302" };
            ViewBag.StatusList = new List<string> { "Active", "Inactive" };

            var result = await redirects
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(result);
        }

        // CREATE REDIRECT - GET
        [HttpGet]
        public IActionResult CreateRedirect()
        {
            return View();
        }

        // CREATE REDIRECT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRedirect(Redirect redirect)
        {
            redirect.SourceUrl = redirect.SourceUrl?.Trim() ?? string.Empty;
            redirect.DestinationUrl = redirect.DestinationUrl?.Trim() ?? string.Empty;

            if (redirect.RedirectType != "301" &&
                redirect.RedirectType != "302")
            {
                ModelState.AddModelError(
                    nameof(redirect.RedirectType),
                    "Redirect type must be 301 or 302.");
            }

            if (redirect.SourceUrl.Equals(
                redirect.DestinationUrl,
                StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(redirect.DestinationUrl),
                    "Source and destination URLs cannot be the same.");
            }

            if (await _context.Redirects.AnyAsync(r =>
                r.SourceUrl == redirect.SourceUrl))
            {
                ModelState.AddModelError(
                    nameof(redirect.SourceUrl),
                    "This source URL already exists.");
            }

            if (ModelState.IsValid)
            {
                redirect.CreatedAt = DateTime.Now;
                redirect.UpdatedAt = null;

                _context.Redirects.Add(redirect);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Redirect created successfully.";
                return RedirectToAction(nameof(Redirects));
            }

            return View(redirect);
        }

        // EDIT REDIRECT - GET
        [HttpGet]
        public async Task<IActionResult> EditRedirect(int id)
        {
            var redirect = await _context.Redirects.FindAsync(id);

            if (redirect == null)
                return NotFound();

            return View(redirect);
        }

        // EDIT REDIRECT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRedirect(int id, Redirect redirect)
        {
            if (id != redirect.Id)
                return BadRequest();

            redirect.SourceUrl = redirect.SourceUrl?.Trim() ?? string.Empty;
            redirect.DestinationUrl = redirect.DestinationUrl?.Trim() ?? string.Empty;

            if (redirect.RedirectType != "301" &&
                redirect.RedirectType != "302")
            {
                ModelState.AddModelError(
                    nameof(redirect.RedirectType),
                    "Redirect type must be 301 or 302.");
            }

            if (redirect.SourceUrl.Equals(
                redirect.DestinationUrl,
                StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(redirect.DestinationUrl),
                    "Source and destination URLs cannot be the same.");
            }

            if (await _context.Redirects.AnyAsync(r =>
                r.SourceUrl == redirect.SourceUrl && r.Id != id))
            {
                ModelState.AddModelError(
                    nameof(redirect.SourceUrl),
                    "This source URL already exists.");
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.Redirects.FindAsync(id);

                if (existing == null)
                    return NotFound();

                existing.SourceUrl = redirect.SourceUrl;
                existing.DestinationUrl = redirect.DestinationUrl;
                existing.RedirectType = redirect.RedirectType;
                existing.IsActive = redirect.IsActive;
                existing.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Redirect updated successfully.";
                return RedirectToAction(nameof(Redirects));
            }

            return View(redirect);
        }

        // REDIRECT DETAILS
        [HttpGet]
        public async Task<IActionResult> RedirectDetails(int id)
        {
            var redirect = await _context.Redirects
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (redirect == null)
                return NotFound();

            return View(redirect);
        }

        // DELETE REDIRECT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRedirect(int id)
        {
            var redirect = await _context.Redirects.FindAsync(id);

            if (redirect == null)
            {
                TempData["Error"] = "Redirect not found.";
                return RedirectToAction(nameof(Redirects));
            }

            _context.Redirects.Remove(redirect);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Redirect deleted successfully.";
            return RedirectToAction(nameof(Redirects));
        }

        // ACTIVATE REDIRECT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateRedirect(int id)
        {
            var redirect = await _context.Redirects.FindAsync(id);

            if (redirect == null)
                return NotFound();

            redirect.IsActive = true;
            redirect.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Redirect activated successfully.";
            return RedirectToAction(nameof(Redirects));
        }

        // DEACTIVATE REDIRECT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateRedirect(int id)
        {
            var redirect = await _context.Redirects.FindAsync(id);

            if (redirect == null)
                return NotFound();

            redirect.IsActive = false;
            redirect.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Redirect deactivated successfully.";
            return RedirectToAction(nameof(Redirects));
        }

        // =====================================================
        // SEO SETTINGS MANAGEMENT
        // =====================================================

        // LIST + SEARCH + FILTER
        [HttpGet]
        public async Task<IActionResult> SeoSettings(
            string searchString,
            string contentType)
        {
            var seoSettings = _context.SeoSettings
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                seoSettings = seoSettings.Where(s =>
                    s.SeoTitle.Contains(searchString) ||
                    (s.MetaDescription != null &&
                     s.MetaDescription.Contains(searchString)) ||
                    (s.CanonicalUrl != null &&
                     s.CanonicalUrl.Contains(searchString)));
            }

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                seoSettings = seoSettings.Where(s =>
                    s.ContentType == contentType);
            }

            ViewBag.SearchString = searchString;
            ViewBag.ContentType = contentType;

            ViewBag.ContentTypes = new List<string>
    {
        "Page", "Blog", "Project", "Product", "Service"
    };

            var result = await seoSettings
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(result);
        }

        // CREATE - GET
        [HttpGet]
        public IActionResult CreateSeoSetting()
        {
            return View();
        }

        // CREATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSeoSetting(SeoSetting seoSetting)
        {
            if (seoSetting.ContentId <= 0)
            {
                ModelState.AddModelError(
                    nameof(seoSetting.ContentId),
                    "Content ID must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(seoSetting.ContentType))
            {
                ModelState.AddModelError(
                    nameof(seoSetting.ContentType),
                    "Please select content type.");
            }

            bool duplicate = await _context.SeoSettings.AnyAsync(s =>
                s.ContentType == seoSetting.ContentType &&
                s.ContentId == seoSetting.ContentId);

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(seoSetting.ContentId),
                    "SEO settings already exist for this content.");
            }

            if (ModelState.IsValid)
            {
                seoSetting.CreatedAt = DateTime.Now;
                seoSetting.UpdatedAt = null;

                _context.SeoSettings.Add(seoSetting);
                await _context.SaveChangesAsync();

                TempData["Success"] = "SEO settings created successfully.";
                return RedirectToAction(nameof(SeoSettings));
            }

            return View(seoSetting);
        }

        // EDIT - GET
        [HttpGet]
        public async Task<IActionResult> EditSeoSetting(int id)
        {
            var seoSetting = await _context.SeoSettings.FindAsync(id);

            if (seoSetting == null)
                return NotFound();

            return View(seoSetting);
        }

        // EDIT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSeoSetting(
            int id,
            SeoSetting seoSetting)
        {
            if (id != seoSetting.Id)
                return BadRequest();

            if (seoSetting.ContentId <= 0)
            {
                ModelState.AddModelError(
                    nameof(seoSetting.ContentId),
                    "Content ID must be greater than zero.");
            }

            bool duplicate = await _context.SeoSettings.AnyAsync(s =>
                s.ContentType == seoSetting.ContentType &&
                s.ContentId == seoSetting.ContentId &&
                s.Id != id);

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(seoSetting.ContentId),
                    "SEO settings already exist for this content.");
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.SeoSettings.FindAsync(id);

                if (existing == null)
                    return NotFound();

                existing.ContentType = seoSetting.ContentType;
                existing.ContentId = seoSetting.ContentId;
                existing.SeoTitle = seoSetting.SeoTitle;
                existing.MetaDescription = seoSetting.MetaDescription;
                existing.MetaKeywords = seoSetting.MetaKeywords;
                existing.CanonicalUrl = seoSetting.CanonicalUrl;
                existing.Robots = seoSetting.Robots;
                existing.OgTitle = seoSetting.OgTitle;
                existing.OgDescription = seoSetting.OgDescription;
                existing.OgImage = seoSetting.OgImage;
                existing.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "SEO settings updated successfully.";
                return RedirectToAction(nameof(SeoSettings));
            }

            return View(seoSetting);
        }

        // DETAILS
        [HttpGet]
        public async Task<IActionResult> SeoSettingDetails(int id)
        {
            var seoSetting = await _context.SeoSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seoSetting == null)
                return NotFound();

            return View(seoSetting);
        }

        // DELETE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeoSetting(int id)
        {
            var seoSetting = await _context.SeoSettings.FindAsync(id);

            if (seoSetting == null)
            {
                TempData["Error"] = "SEO setting not found.";
                return RedirectToAction(nameof(SeoSettings));
            }

            _context.SeoSettings.Remove(seoSetting);
            await _context.SaveChangesAsync();

            TempData["Success"] = "SEO settings deleted successfully.";
            return RedirectToAction(nameof(SeoSettings));
        }

        // =====================================================
        // SETTINGS MANAGEMENT
        // =====================================================

        // SETTINGS PAGE - GET
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var setting = await _context.Settings
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync();

            // First visit par empty settings object show hoga.
            if (setting == null)
            {
                setting = new Setting();
            }

            return View(setting);
        }

        // SAVE SETTINGS - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(Setting setting)
        {
            if (!ModelState.IsValid)
            {
                return View("Settings", setting);
            }

            var existing = await _context.Settings
                .FirstOrDefaultAsync(s => s.Id == setting.Id);

            if (existing == null && setting.Id != 0)
            {
                TempData["Error"] = "Settings record not found.";
                return RedirectToAction(nameof(Settings));
            }

            if (existing == null)
            {
                setting.Id = 0;
                setting.CreatedAt = DateTime.Now;
                setting.UpdatedAt = null;

                _context.Settings.Add(setting);
            }
            else
            {
                existing.SiteName = setting.SiteName;
                existing.SiteTagline = setting.SiteTagline;
                existing.Logo = setting.Logo;
                existing.Favicon = setting.Favicon;

                existing.Email = setting.Email;
                existing.Phone = setting.Phone;
                existing.Address = setting.Address;
                existing.City = setting.City;
                existing.State = setting.State;
                existing.PostalCode = setting.PostalCode;
                existing.Country = setting.Country;

                existing.BusinessHours = setting.BusinessHours;

                existing.FacebookUrl = setting.FacebookUrl;
                existing.InstagramUrl = setting.InstagramUrl;
                existing.TwitterUrl = setting.TwitterUrl;
                existing.LinkedInUrl = setting.LinkedInUrl;
                existing.YouTubeUrl = setting.YouTubeUrl;

                existing.PrimaryColor = setting.PrimaryColor;
                existing.SecondaryColor = setting.SecondaryColor;

                existing.MaintenanceMode = setting.MaintenanceMode;
                existing.EnableNotifications = setting.EnableNotifications;
                existing.TimeZone = setting.TimeZone;

                existing.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Settings saved successfully.";

            return RedirectToAction(nameof(Settings));
        }

        // =====================================================
        // TRASH MANAGEMENT
        // =====================================================

        // LIST + SEARCH + MODULE FILTER
        [HttpGet]
        public async Task<IActionResult> Trash(
            string searchString,
            string module,
            string status)
        {
            var items = _context.TrashItems
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                items = items.Where(t =>
                    (t.ItemName != null &&
                     t.ItemName.Contains(searchString)) ||
                    t.ItemId.Contains(searchString) ||
                    t.Module.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(module))
            {
                items = items.Where(t => t.Module == module);
            }

            if (status == "Restored")
                items = items.Where(t => t.IsRestored);
            else if (status == "Deleted")
                items = items.Where(t => !t.IsRestored);

            ViewBag.SearchString = searchString;
            ViewBag.Module = module;
            ViewBag.Status = status;

            ViewBag.Modules = await _context.TrashItems
                .Select(t => t.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            ViewBag.StatusList = new List<string>
    {
        "Deleted", "Restored"
    };

            var result = await items
                .OrderByDescending(t => t.DeletedAt)
                .ToListAsync();

            return View(result);
        }

        // TRASH DETAILS
        [HttpGet]
        public async Task<IActionResult> TrashDetails(int id)
        {
            var item = await _context.TrashItems
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // RESTORE TRASH ITEM
        // NOTE: Existing RestoreTrash action ho to duplicate mat add karna.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreTrash(int id)
        {
            var item = await _context.TrashItems.FindAsync(id);

            if (item == null)
            {
                TempData["Error"] = "Trash item not found.";
                return RedirectToAction(nameof(Trash));
            }

            if (item.IsRestored)
            {
                TempData["Error"] = "This item has already been restored.";
                return RedirectToAction(nameof(Trash));
            }

            // IsRestored ko yahan tabhi update karna safe hai,
            // jab actual module record successfully restore ho chuka ho.
            // Existing module-specific restore logic ko preserve karo.

            TempData["Error"] =
                "Use the existing module-specific restore logic for this item.";

            return RedirectToAction(nameof(Trash));
        }

        // PERMANENT DELETE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDeleteTrash(int id)
        {
            var item = await _context.TrashItems.FindAsync(id);

            if (item == null)
            {
                TempData["Error"] = "Trash item not found.";
                return RedirectToAction(nameof(Trash));
            }

            _context.TrashItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Trash record permanently deleted.";
            return RedirectToAction(nameof(Trash));
        }
        // =====================================================
        // REPORTS
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Reports()
        {
            return View();
        }


        // =====================================================
        // AUDIT LOGS
        // =====================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AuditLogs()
        {
            var auditLogs = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return View(auditLogs);
        }


        // =====================================================
        // FORM SUBMISSIONS MANAGEMENT
        // =====================================================

        // GET: /CMS/FormSubmissions
        [HttpGet]
        public async Task<IActionResult> FormSubmissions(
            string? searchString,
            int? formId,
            string? status)
        {
            var query = _context.FormSubmissions
                .Include(x => x.Form)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                query = query.Where(x =>
                    x.Name.Contains(searchString) ||
                    x.Email.Contains(searchString) ||
                    (x.Phone != null && x.Phone.Contains(searchString)));
            }

            if (formId.HasValue)
            {
                query = query.Where(x => x.FormId == formId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            ViewBag.Forms = await _context.Forms
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.FormId = formId;
            ViewBag.Status = status;

            var submissions = await query
                .OrderByDescending(x => x.SubmittedAt)
                .ToListAsync();

            return View(submissions);
        }


        // GET: /CMS/CreateFormSubmission

        [HttpGet]
        public async Task<IActionResult> CreateFormSubmission()
        {
            ViewBag.Forms = await _context.Forms
                .OrderBy(f => f.Name)
                .ToListAsync();

            return View();
        }


        // POST: /CMS/CreateFormSubmission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFormSubmission(
            [Bind("FormId,Name,Email,Phone,SubmissionData,Status,Remarks")]
            FormSubmission submission)
        {
            if (!await _context.Forms.AnyAsync(
                x => x.Id == submission.FormId))
            {
                ModelState.AddModelError(
                    "FormId",
                    "Please select a valid form.");
            }

            if (ModelState.IsValid)
            {
                submission.SubmittedAt = DateTime.Now;
                submission.UpdatedAt = null;

                if (string.IsNullOrWhiteSpace(submission.Status))
                {
                    submission.Status = "New";
                }

                _context.FormSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Form submission created successfully.";

                return RedirectToAction(nameof(FormSubmissions));
            }

            await LoadSubmissionFormsAsync(submission.FormId);

            return View(submission);
        }


        // GET: /CMS/FormSubmissionDetails/5
        [HttpGet]
        public async Task<IActionResult> FormSubmissionDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.FormSubmissions
                .Include(x => x.Form)
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }


        // GET: /CMS/EditFormSubmission/5
        [HttpGet]
        public async Task<IActionResult> EditFormSubmission(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.FormSubmissions
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (submission == null)
            {
                return NotFound();
            }

            await LoadSubmissionFormsAsync(submission.FormId);

            return View(submission);
        }


        // POST: /CMS/EditFormSubmission/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFormSubmission(
            int id,
            [Bind("Id,FormId,Name,Email,Phone,SubmissionData,Status,Remarks")]
            FormSubmission submission)
        {
            if (id != submission.Id)
            {
                return NotFound();
            }

            if (!await _context.Forms.AnyAsync(
                x => x.Id == submission.FormId))
            {
                ModelState.AddModelError(
                    "FormId",
                    "Please select a valid form.");
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.FormSubmissions
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (existing == null)
                {
                    return NotFound();
                }

                existing.FormId = submission.FormId;
                existing.Name = submission.Name;
                existing.Email = submission.Email;
                existing.Phone = submission.Phone;
                existing.SubmissionData = submission.SubmissionData;
                existing.Status = submission.Status;
                existing.Remarks = submission.Remarks;
                existing.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Form submission updated successfully.";

                return RedirectToAction(nameof(FormSubmissions));
            }

            await LoadSubmissionFormsAsync(submission.FormId);

            return View(submission);
        }


        // GET: /CMS/DeleteFormSubmission/5
        [HttpGet]
        public async Task<IActionResult> DeleteFormSubmission(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.FormSubmissions
                .Include(x => x.Form)
                .FirstOrDefaultAsync(x => x.Id == id.Value);

            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }


        // POST: /CMS/DeleteFormSubmission/5
        [HttpPost, ActionName("DeleteFormSubmission")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFormSubmissionConfirmed(int id)
        {
            var submission = await _context.FormSubmissions
                .FirstOrDefaultAsync(x => x.Id == id);

            if (submission == null)
            {
                return NotFound();
            }

            _context.FormSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Form submission deleted successfully.";

            return RedirectToAction(nameof(FormSubmissions));
        }


        // Helper: load forms for Create and Edit dropdowns
        private async Task LoadSubmissionFormsAsync(int? selectedFormId = null)
        {
            ViewBag.Forms = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Forms
                    .OrderBy(x => x.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                selectedFormId);
        }


        // =====================================================
        // MENU ITEMS MANAGEMENT
        // =====================================================

        // 1. MENU ITEMS LIST + SEARCH + LOCATION FILTER

        [HttpGet]
        public async Task<IActionResult> MenuItems(
            string? searchString,
            string? location)
        {
            var query = _context.MenuItems
                .Include(m => m.Parent)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(m =>
                    m.Title.Contains(searchString) ||
                    m.Url.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(m => m.Location == location);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Location = location;

            var menuItems = await query
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Title)
                .ToListAsync();

            return View(menuItems);
        }


        // 2. CREATE MENU ITEM - GET

        [HttpGet]
        public async Task<IActionResult> CreateMenuItem()
        {
            await LoadMenuItemParentsAsync();

            return View(new MenuItem());
        }


        // 3. CREATE MENU ITEM - POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem(MenuItem menuItem)
        {
            if (menuItem.ParentId.HasValue)
            {
                bool parentExists = await _context.MenuItems
                    .AnyAsync(m => m.Id == menuItem.ParentId.Value);

                if (!parentExists)
                {
                    ModelState.AddModelError(
                        nameof(menuItem.ParentId),
                        "Selected parent menu does not exist.");
                }
            }

            if (ModelState.IsValid)
            {
                menuItem.Title = menuItem.Title.Trim();
                menuItem.Url = menuItem.Url.Trim();
                menuItem.Location = menuItem.Location.Trim();
                menuItem.CreatedAt = DateTime.Now;

                _context.MenuItems.Add(menuItem);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Menu item created successfully!";

                return RedirectToAction(nameof(MenuItems));
            }

            await LoadMenuItemParentsAsync(menuItem.Id);

            return View(menuItem);
        }


        // 4. EDIT MENU ITEM - GET

        [HttpGet]
        public async Task<IActionResult> EditMenuItem(int id)
        {
            var menuItem = await _context.MenuItems
                .FindAsync(id);

            if (menuItem == null)
            {
                return NotFound();
            }

            await LoadMenuItemParentsAsync(id, menuItem.ParentId);

            return View(menuItem);
        }


        // 5. EDIT MENU ITEM - POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMenuItem(
            int id,
            MenuItem menuItem)
        {
            if (id != menuItem.Id)
            {
                return BadRequest();
            }

            var existingMenuItem = await _context.MenuItems
                .FindAsync(id);

            if (existingMenuItem == null)
            {
                return NotFound();
            }

            // Prevent a menu item from being its own parent.
            if (menuItem.ParentId == id)
            {
                ModelState.AddModelError(
                    nameof(menuItem.ParentId),
                    "A menu item cannot be its own parent.");
            }

            // Validate the selected parent.
            if (menuItem.ParentId.HasValue)
            {
                bool parentExists = await _context.MenuItems
                    .AnyAsync(m => m.Id == menuItem.ParentId.Value);

                if (!parentExists)
                {
                    ModelState.AddModelError(
                        nameof(menuItem.ParentId),
                        "Selected parent menu does not exist.");
                }
            }

            if (ModelState.IsValid)
            {
                existingMenuItem.Title = menuItem.Title.Trim();
                existingMenuItem.Location = menuItem.Location.Trim();
                existingMenuItem.Url = menuItem.Url.Trim();
                existingMenuItem.Description = menuItem.Description;
                existingMenuItem.ParentId = menuItem.ParentId;
                existingMenuItem.DisplayOrder = menuItem.DisplayOrder;
                existingMenuItem.IsVisible = menuItem.IsVisible;
                existingMenuItem.Target = menuItem.Target;
                existingMenuItem.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Menu item updated successfully!";

                return RedirectToAction(nameof(MenuItems));
            }

            await LoadMenuItemParentsAsync(id, menuItem.ParentId);

            return View(menuItem);
        }


        // 6. MENU ITEM DETAILS

        [HttpGet]
        public async Task<IActionResult> MenuItemDetails(int id)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }


        // 7. DELETE MENU ITEM - CONFIRMATION PAGE

        [HttpGet]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }


        // 8. DELETE MENU ITEM - POST

        [HttpPost]
        [ActionName("DeleteMenuItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItemConfirmed(int id)
        {
            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null)
            {
                TempData["Error"] = "Menu item was not found.";
                return RedirectToAction(nameof(MenuItems));
            }

            // Do not delete a parent menu that has child items.
            bool hasChildren = await _context.MenuItems
                .AnyAsync(m => m.ParentId == id);

            if (hasChildren)
            {
                TempData["Error"] =
                    "This menu has child items. Delete or reassign them first.";

                return RedirectToAction(nameof(MenuItems));
            }

            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item deleted successfully!";

            return RedirectToAction(nameof(MenuItems));
        }


        // 9. HELPER: LOAD PARENT MENUS FOR DROPDOWNS

        private async Task LoadMenuItemParentsAsync(
            int? excludeId = null,
            int? selectedParentId = null)
        {
            var parentsQuery = _context.MenuItems.AsQueryable();

            if (excludeId.HasValue)
            {
                parentsQuery = parentsQuery
                    .Where(m => m.Id != excludeId.Value);
            }

            var parents = await parentsQuery
                .OrderBy(m => m.Title)
                .ToListAsync();

            ViewBag.ParentMenuItems = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                parents,
                "Id",
                "Title",
                selectedParentId);
        }

        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            // Load the latest saved site settings if available.
            // Adjust property names if your Setting model uses different names.
            var settings = await _context.Settings.FirstOrDefaultAsync();

            ViewBag.SiteName = settings?.SiteName ?? "";
            ViewBag.Email = settings?.Email ?? "";
            ViewBag.Phone = settings?.Phone ?? "";
            ViewBag.Address = settings?.Address ?? "";
            ViewBag.BusinessHours = settings?.BusinessHours ?? "";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitContact(
            string? name,
            string? email,
            string? phone,
            string? subject,
            string? message)
        {
            name = name?.Trim();
            email = email?.Trim();
            phone = phone?.Trim();
            subject = subject?.Trim();
            message = message?.Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] =
                    "Please enter your name, email and message.";

                return RedirectToAction(nameof(Contact));
            }

            if (!new System.ComponentModel.DataAnnotations
                    .EmailAddressAttribute().IsValid(email))
            {
                TempData["ErrorMessage"] =
                    "Please enter a valid email address.";

                return RedirectToAction(nameof(Contact));
            }

            // IMPORTANT:
            // Replace ContactSubmission and its properties below
            // with the exact names in your existing model, if different.
            var submission = new ContactSubmission
            {
                Name = name,
                Email = email,
                Phone = phone,
                Subject = subject,
                Message = message,
                Status = "New",
                SubmittedAt = DateTime.Now
            };

            _context.ContactSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your message has been submitted successfully.";

            return RedirectToAction(nameof(Contact));
        }


        // GET: /JobApplication
        // GET: /JobApplication/JobApplications
        [HttpGet]
        public async Task<IActionResult> JobApplications(
            string? searchString,
            string? status)
        {
            var applications = _context.JobApplications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                applications = applications.Where(a =>
                    a.ApplicantName.Contains(searchString) ||
                    a.Email.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                applications = applications.Where(a => a.Status == status);
            }

            ViewBag.SearchString = searchString;
            ViewBag.SelectedStatus = status;

            var result = await applications
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(result);
        }

        // GET: /JobApplication/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // POST: /JobApplication/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            string[] allowedStatuses =
            {
                "Pending",
                "Reviewed",
                "Shortlisted",
                "Rejected",
                "Hired"
            };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowedStatuses.Contains(status))
            {
                return BadRequest("Invalid application status.");
            }

            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            application.Status = status;
            application.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Application status updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = application.Id });
        }

        // POST: /JobApplication/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            _context.JobApplications.Remove(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Job application deleted successfully.";

            return RedirectToAction(nameof(JobApplications));

        }

        [HttpGet]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> JobApplicationDetails(int id)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

       

        // =====================================================
        // EDIT JOB APPLICATION - GET
        // URL: /CMS/EditJobApplication/3
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> EditJobApplication(int id)

        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound();

            return View(application);
        }


        // =====================================================
        // EDIT JOB APPLICATION - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> EditJobApplication(
            int id,
            string applicantName,
            string email,
            string? phone,
            string? status,
            string? notes)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(applicantName) ||
                string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "",
                    "Applicant name and email are required.");

                return View(application);
            }

            string[] allowedStatuses =
              {
                "Pending",
                "Reviewed",
                "Shortlisted",
                "Rejected",
                "Hired"
               };


            if (!string.IsNullOrWhiteSpace(status) &&
                !allowedStatuses.Contains(status))
            {
                ModelState.AddModelError("", "Invalid application status.");
                return View(application);
            }

            application.ApplicantName = applicantName.Trim();
            application.Email = email.Trim();
            application.Phone = phone?.Trim();
            

            if (!string.IsNullOrWhiteSpace(status))
                application.Status = status;

            application.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Job application updated successfully.";

            return RedirectToAction("JobApplications");
        }

        // =====================================================
        // DELETE JOB APPLICATION - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> DeleteJobApplication(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid job application ID.";
                return RedirectToAction(nameof(JobApplications));
            }

            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                TempData["Error"] = "Job application not found.";
                return RedirectToAction(nameof(JobApplications));
            }

            _context.JobApplications.Remove(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Job application deleted successfully.";

            return RedirectToAction(nameof(JobApplications));
        }





    }
}
    



    






