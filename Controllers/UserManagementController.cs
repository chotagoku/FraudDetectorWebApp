using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using FraudDetectorWebApp.Services;
using FraudDetectorWebApp.DTOs;
using FraudDetectorWebApp.Attributes;
using System.Security.Claims;
using BCrypt.Net;

namespace FraudDetectorWebApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [RequirePermission("USER_MANAGEMENT")]
    public class UserManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(ApplicationDbContext context, IPermissionService permissionService,
            ILogger<UserManagementController> logger)
        {
            _context = context;
            _permissionService = permissionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? search = null, 
            [FromQuery] string? role = null, [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => u.FirstName.Contains(search) || 
                                           u.LastName.Contains(search) || 
                                           u.Email.Contains(search) || 
                                           (u.Company != null && u.Company.Contains(search)));
                }

                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(u => u.Role == role);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = new List<UserManagementDto>();

                foreach (var user in users)
                {
                    var userRoles = await _permissionService.GetUserRolesAsync(user.Id);
                    var userPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);

                    userDtos.Add(new UserManagementDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Phone = user.Phone,
                        Company = user.Company,
                        Role = user.Role,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        AssignedRoles = userRoles,
                        Permissions = userPermissions
                    });
                }

                var response = new PaginatedUserResponseDto<UserManagementDto>
                {
                    Items = userDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Filters = new Dictionary<string, object>
                    {
                        ["search"] = search ?? "",
                        ["role"] = role ?? "",
                        ["isActive"] = isActive
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to retrieve users",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new UserManagementResponseDto<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var userRoles = await _permissionService.GetUserRolesAsync(user.Id);
                var userPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);

                var userDto = new UserManagementDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Company = user.Company,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    AssignedRoles = userRoles,
                    Permissions = userPermissions
                };

                return Ok(new UserManagementResponseDto<UserManagementDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to retrieve user",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost]
        [AuditAdminAction("CREATE_USER", "USER")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == createUserDto.Email);
                if (existingUser != null)
                {
                    return Conflict(new UserManagementResponseDto<object>
                    {
                        Success = false,
                        Message = "A user with this email address already exists"
                    });
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

                var user = new User
                {
                    FirstName = createUserDto.FirstName,
                    LastName = createUserDto.LastName,
                    Email = createUserDto.Email,
                    PasswordHash = passwordHash,
                    Phone = createUserDto.Phone,
                    Company = createUserDto.Company,
                    Role = "User", // Default role
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var currentUserId = GetCurrentUserId();

                // Assign roles if specified
                foreach (var roleName in createUserDto.Roles)
                {
                    await _permissionService.AssignRoleToUserAsync(user.Id, roleName, currentUserId);
                }

                // Grant permissions if specified
                foreach (var permission in createUserDto.Permissions)
                {
                    await _permissionService.GrantPermissionToUserAsync(user.Id, permission, currentUserId);
                }

                await _permissionService.LogAdminActionAsync(
                    currentUserId, "CREATE_USER", "USER", user.Id, user.Email,
                    newValue: new { user.Email, user.FirstName, user.LastName, Roles = createUserDto.Roles },
                    description: $"Created user {user.Email}",
                    ipAddress: GetClientIpAddress(),
                    userAgent: GetUserAgent());

                return Ok(new UserManagementResponseDto<object>
                {
                    Success = true,
                    Message = $"User {user.Email} created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to create user",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("{id}")]
        [ValidateUserModification]
        [AuditAdminAction("UPDATE_USER", "USER")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new UserManagementResponseDto<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var oldValues = new { user.FirstName, user.LastName, user.Email, user.Phone, user.Company, user.IsActive };

                user.FirstName = updateUserDto.FirstName;
                user.LastName = updateUserDto.LastName;
                user.Email = updateUserDto.Email;
                user.Phone = updateUserDto.Phone;
                user.Company = updateUserDto.Company;
                user.IsActive = updateUserDto.IsActive;

                await _context.SaveChangesAsync();

                var currentUserId = GetCurrentUserId();
                await _permissionService.LogAdminActionAsync(
                    currentUserId, "UPDATE_USER", "USER", user.Id, user.Email,
                    oldValue: oldValues,
                    newValue: new { user.FirstName, user.LastName, user.Email, user.Phone, user.Company, user.IsActive },
                    description: $"Updated user {user.Email}",
                    ipAddress: GetClientIpAddress(),
                    userAgent: GetUserAgent());

                return Ok(new UserManagementResponseDto<object>
                {
                    Success = true,
                    Message = "User updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to update user",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{id}/assign-role")]
        [ValidateUserModification]
        [AuditAdminAction("ASSIGN_ROLE", "USER")]
        public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleDto assignRoleDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new UserManagementResponseDto<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var currentUserId = GetCurrentUserId();
                var success = await _permissionService.AssignRoleToUserAsync(id, assignRoleDto.RoleName, currentUserId);

                if (!success)
                {
                    return BadRequest(new UserManagementResponseDto<object>
                    {
                        Success = false,
                        Message = $"Failed to assign role {assignRoleDto.RoleName}"
                    });
                }

                await _permissionService.LogAdminActionAsync(
                    currentUserId, "ASSIGN_ROLE", "USER", user.Id, user.Email,
                    newValue: new { Role = assignRoleDto.RoleName },
                    description: $"Assigned role {assignRoleDto.RoleName} to user {user.Email}",
                    ipAddress: GetClientIpAddress(),
                    userAgent: GetUserAgent());

                return Ok(new UserManagementResponseDto<object>
                {
                    Success = true,
                    Message = $"Role {assignRoleDto.RoleName} assigned successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user {UserId}", id);
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to assign role",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{id}/remove-role")]
        [ValidateUserModification]
        [AuditAdminAction("REMOVE_ROLE", "USER")]
        public async Task<IActionResult> RemoveRole(int id, [FromBody] RemoveRoleDto removeRoleDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new UserManagementResponseDto<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var currentUserId = GetCurrentUserId();
                var success = await _permissionService.RemoveRoleFromUserAsync(id, removeRoleDto.RoleName, currentUserId);

                if (!success)
                {
                    return BadRequest(new UserManagementResponseDto<object>
                    {
                        Success = false,
                        Message = $"Failed to remove role {removeRoleDto.RoleName}"
                    });
                }

                await _permissionService.LogAdminActionAsync(
                    currentUserId, "REMOVE_ROLE", "USER", user.Id, user.Email,
                    oldValue: new { Role = removeRoleDto.RoleName },
                    description: $"Removed role {removeRoleDto.RoleName} from user {user.Email}",
                    ipAddress: GetClientIpAddress(),
                    userAgent: GetUserAgent());

                return Ok(new UserManagementResponseDto<object>
                {
                    Success = true,
                    Message = $"Role {removeRoleDto.RoleName} removed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user {UserId}", id);
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to remove role",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        IsSystemRole = r.IsSystemRole,
                        IsActive = r.IsActive,
                        CreatedAt = r.CreatedAt,
                        UserCount = r.UserRoles.Count(),
                        Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
                    })
                    .ToListAsync();

                return Ok(new UserManagementResponseDto<List<RoleDto>>
                {
                    Success = true,
                    Message = "Roles retrieved successfully",
                    Data = roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to retrieve roles",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions()
        {
            try
            {
                var permissions = await _context.Permissions
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Category = p.Category,
                        ResourcePath = p.ResourcePath,
                        IsSystemPermission = p.IsSystemPermission,
                        CreatedAt = p.CreatedAt
                    })
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                return Ok(new UserManagementResponseDto<List<PermissionDto>>
                {
                    Success = true,
                    Message = "Permissions retrieved successfully",
                    Data = permissions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions");
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to retrieve permissions",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("audit-logs")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AdminActionLogFilterDto filter)
        {
            try
            {
                var query = _context.AdminActionLogs
                    .Include(log => log.AdminUser)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(filter.Action))
                {
                    query = query.Where(log => log.Action.Contains(filter.Action));
                }

                if (!string.IsNullOrEmpty(filter.TargetType))
                {
                    query = query.Where(log => log.TargetType == filter.TargetType);
                }

                if (!string.IsNullOrEmpty(filter.AdminUserName))
                {
                    query = query.Where(log => log.AdminUser.FirstName.Contains(filter.AdminUserName) ||
                                              log.AdminUser.LastName.Contains(filter.AdminUserName) ||
                                              log.AdminUser.Email.Contains(filter.AdminUserName));
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(log => log.ActionAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(log => log.ActionAt <= filter.EndDate.Value);
                }

                if (filter.IsSuccessful.HasValue)
                {
                    query = query.Where(log => log.IsSuccessful == filter.IsSuccessful.Value);
                }

                var totalCount = await query.CountAsync();

                // Apply sorting
                query = filter.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(log => EF.Property<object>(log, filter.SortBy))
                    : query.OrderByDescending(log => EF.Property<object>(log, filter.SortBy));

                var logs = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(log => new AdminActionLogDto
                    {
                        Id = log.Id,
                        AdminUserName = $"{log.AdminUser.FirstName} {log.AdminUser.LastName}",
                        Action = log.Action,
                        TargetType = log.TargetType,
                        TargetId = log.TargetId,
                        TargetName = log.TargetName,
                        OldValue = log.OldValue,
                        NewValue = log.NewValue,
                        Description = log.Description,
                        IpAddress = log.IpAddress,
                        ActionAt = log.ActionAt,
                        IsSuccessful = log.IsSuccessful,
                        ErrorMessage = log.ErrorMessage
                    })
                    .ToListAsync();

                var response = new PaginatedUserResponseDto<AdminActionLogDto>
                {
                    Items = logs,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return StatusCode(500, new UserManagementResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to retrieve audit logs",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers.UserAgent.ToString();
        }
    }
}
