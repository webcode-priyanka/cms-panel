using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSPanel.Models
{
    public class RolePermission
    {
        public int Id { get; set; }

        [Required]
        public string RoleId { get; set; } = string.Empty;

        [ForeignKey("RoleId")]
        public virtual Microsoft.AspNetCore.Identity.IdentityRole? Role { get; set; }

        [Required]
        public int PermissionId { get; set; }

        [ForeignKey("PermissionId")]
        public virtual Permission? Permission { get; set; }

        public bool IsAllowed { get; set; } = true;
    }
}
