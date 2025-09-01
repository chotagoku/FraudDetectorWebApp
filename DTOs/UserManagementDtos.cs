using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.DTOs
{
    // User Management DTOs
    public class UserManagementDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> AssignedRoles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string FullName => $"{FirstName} {LastName}";
    }

    public class CreateUserDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
        public string Password { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Company { get; set; }

        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateUserDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Company { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required.")]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AssignRoleDto
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
    }

    public class RemoveRoleDto
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
    }

    public class GrantPermissionDto
    {
        [Required]
        public string Permission { get; set; } = string.Empty;
    }

    public class RevokePermissionDto
    {
        [Required]
        public string Permission { get; set; } = string.Empty;
    }

    // Role Management DTOs
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Permissions { get; set; } = new();
        public int UserCount { get; set; }
    }

    public class CreateRoleDto
    {
        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 50 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; }

        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateRoleDto
    {
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public List<string> Permissions { get; set; } = new();
    }

    // Permission DTOs
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ResourcePath { get; set; } = string.Empty;
        public bool IsSystemPermission { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePermissionDto
    {
        [Required(ErrorMessage = "Permission name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Permission name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Resource path is required.")]
        [StringLength(100, ErrorMessage = "Resource path cannot exceed 100 characters.")]
        public string ResourcePath { get; set; } = string.Empty;
    }

    // Service Management DTOs
    public class ServiceInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StartType { get; set; } = string.Empty;
        public bool CanStart { get; set; }
        public bool CanStop { get; set; }
        public bool CanPauseAndContinue { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class ServiceInstallDto
    {
        [Required(ErrorMessage = "Service name is required.")]
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Display name is required.")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Executable path is required.")]
        public string ExecutablePath { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string StartType { get; set; } = "Manual";
    }

    public class ServiceActionDto
    {
        [Required(ErrorMessage = "Action is required.")]
        public string Action { get; set; } = string.Empty; // Start, Stop, Restart, Pause, Continue
    }

    // Admin Action Log DTOs
    public class AdminActionLogDto
    {
        public int Id { get; set; }
        public string AdminUserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public int? TargetId { get; set; }
        public string? TargetName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Description { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public DateTime ActionAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AdminActionLogFilterDto
    {
        public string? Action { get; set; }
        public string? TargetType { get; set; }
        public string? AdminUserName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsSuccessful { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "ActionAt";
        public string SortDirection { get; set; } = "desc";
    }

    // System Status DTOs
    public class SystemStatusDto
    {
        public bool IsHealthy { get; set; }
        public List<string> Services { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public DateTime CheckedAt { get; set; }
    }

    // Response DTOs
    public class UserManagementResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public int? StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class PaginatedUserResponseDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
        public Dictionary<string, object> Filters { get; set; } = new();
    }
}
