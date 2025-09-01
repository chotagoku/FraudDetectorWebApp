using System.ComponentModel.DataAnnotations;
using FraudDetectorWebApp.Attributes;

namespace FraudDetectorWebApp.DTOs
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StrongPassword(MinLength = 8, ErrorMessage = "Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special characters.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [ValidPhoneNumber(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        [Display(Name = "Company")]
        public string? Company { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters.")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "User";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class UserUpdateDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [ValidPhoneNumber(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        [Display(Name = "Company")]
        public string? Company { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters.")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "User";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class UserChangePasswordDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Current password is required.")]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StrongPassword(MinLength = 8, ErrorMessage = "Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special characters.")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class UserLoginDto
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; } = false;
    }

    public class UserSearchDto
    {
        [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters.")]
        [Display(Name = "Search")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Role")]
        public string? Role { get; set; }

        [Display(Name = "Active Only")]
        public bool? IsActive { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        [Display(Name = "Page")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
        [Display(Name = "Page Size")]
        public int PageSize { get; set; } = 10;

        [Display(Name = "Sort By")]
        public string OrderBy { get; set; } = "CreatedAt";

        [Display(Name = "Sort Direction")]
        public string SortDirection { get; set; } = "desc";
    }

    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UserBulkActionDto
    {
        [Required(ErrorMessage = "User IDs are required.")]
        [CollectionCount(MinCount = 1, MaxCount = 100, ErrorMessage = "You must select between 1 and 100 users.")]
        [Display(Name = "User IDs")]
        public List<int> UserIds { get; set; } = new();

        [Required(ErrorMessage = "Action is required.")]
        [StringLength(50, ErrorMessage = "Action cannot exceed 50 characters.")]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty; // "activate", "deactivate", "delete", "changeRole"

        [StringLength(50, ErrorMessage = "Value cannot exceed 50 characters.")]
        [Display(Name = "Value")]
        public string? Value { get; set; } // For role changes, etc.
    }

    public class UserImportDto
    {
        [Required(ErrorMessage = "File is required.")]
        [AllowedExtensions(".csv", ".xlsx", ".json", ErrorMessage = "Only CSV, Excel, and JSON files are allowed.")]
        [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "File size cannot exceed 5 MB.")] // 5MB limit
        [Display(Name = "Import File")]
        public IFormFile File { get; set; } = null!;

        [Display(Name = "Overwrite Existing Users")]
        public bool OverwriteExisting { get; set; } = false;

        [Display(Name = "Send Welcome Emails")]
        public bool SendWelcomeEmails { get; set; } = true;

        [Display(Name = "Default Role")]
        public string DefaultRole { get; set; } = "User";
    }

    public class UserExportDto
    {
        [Display(Name = "Export Format")]
        public string Format { get; set; } = "csv"; // csv, excel, json

        [Display(Name = "Include Inactive Users")]
        public bool IncludeInactiveUsers { get; set; } = true;

        [Display(Name = "Role Filter")]
        public string? RoleFilter { get; set; }

        [Display(Name = "Date From")]
        [NotInFuture(ErrorMessage = "Date cannot be in the future.")]
        public DateTime? DateFrom { get; set; }

        [Display(Name = "Date To")]
        [NotInFuture(ErrorMessage = "Date cannot be in the future.")]
        public DateTime? DateTo { get; set; }
    }

    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int RegularUsers { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new();
        public Dictionary<string, int> UsersByCompany { get; set; } = new();
        public int NewUsersThisMonth { get; set; }
        public int LoginsSinceLastMonth { get; set; }
        public DateTime? LastUserCreated { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
