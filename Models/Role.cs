using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsSystemRole { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // e.g., "Pages", "Services", "API"

        [Required]
        [StringLength(100)]
        public string ResourcePath { get; set; } = string.Empty; // e.g., "/admin/systemlogs", "/api/configuration"

        public bool IsSystemPermission { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }

    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public int RoleId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public int AssignedByUserId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual User AssignedBy { get; set; } = null!;
    }

    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public int AssignedByUserId { get; set; }

        // Navigation properties
        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual User AssignedBy { get; set; } = null!;
    }

    public class UserPermission
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public int PermissionId { get; set; }

        public bool IsGranted { get; set; } = true; // false means explicitly denied

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public int AssignedByUserId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual User AssignedBy { get; set; } = null!;
    }

    // Audit log for admin actions
    public class AdminActionLog
    {
        [Key]
        public int Id { get; set; }

        public int AdminUserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // e.g., "ASSIGN_ROLE", "INSTALL_SERVICE", "MODIFY_CONFIG"

        [Required]
        [StringLength(100)]
        public string TargetType { get; set; } = string.Empty; // e.g., "USER", "SERVICE", "CONFIGURATION"

        public int? TargetId { get; set; }

        [StringLength(255)]
        public string? TargetName { get; set; }

        public string? OldValue { get; set; } // JSON of old state
        public string? NewValue { get; set; } // JSON of new state

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        public DateTime ActionAt { get; set; } = DateTime.UtcNow;

        public bool IsSuccessful { get; set; } = true;

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        // Navigation properties
        public virtual User AdminUser { get; set; } = null!;
    }
}
