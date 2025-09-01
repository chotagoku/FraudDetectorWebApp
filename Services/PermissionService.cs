using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace FraudDetectorWebApp.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int userId, string permission);
        Task<bool> HasRoleAsync(int userId, string role);
        Task<List<string>> GetUserPermissionsAsync(int userId);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<bool> AssignRoleToUserAsync(int userId, string roleName, int assignedByUserId);
        Task<bool> RemoveRoleFromUserAsync(int userId, string roleName, int removedByUserId);
        Task<bool> GrantPermissionToUserAsync(int userId, string permission, int grantedByUserId);
        Task<bool> RevokePermissionFromUserAsync(int userId, string permission, int revokedByUserId);
        Task InitializeSystemPermissionsAsync();
        Task<bool> CanUserPerformServiceOperationsAsync(int userId);
        Task<bool> CanUserManageUsersAsync(int userId);
        Task LogAdminActionAsync(int adminUserId, string action, string targetType, int? targetId = null, 
            string? targetName = null, object? oldValue = null, object? newValue = null, 
            string? description = null, string ipAddress = "", string userAgent = "", bool isSuccessful = true, 
            string? errorMessage = null);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(ApplicationDbContext context, ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            try
            {
                // Check direct user permissions first
                var directPermission = await _context.UserPermissions
                    .Join(_context.Permissions, up => up.PermissionId, p => p.Id, (up, p) => new { up, p })
                    .Where(x => x.up.UserId == userId && x.p.Name == permission)
                    .Select(x => new { x.up.IsGranted })
                    .FirstOrDefaultAsync();

                if (directPermission != null)
                {
                    return directPermission.IsGranted;
                }

                // Check role-based permissions
                var hasRolePermission = await _context.UserRoles
                    .Join(_context.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => new { ur, rp })
                    .Join(_context.Permissions, x => x.rp.PermissionId, p => p.Id, (x, p) => new { x.ur, x.rp, p })
                    .Where(x => x.ur.UserId == userId && x.p.Name == permission)
                    .AnyAsync();

                return hasRolePermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
                return false;
            }
        }

        public async Task<bool> HasRoleAsync(int userId, string role)
        {
            try
            {
                return await _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                    .Where(x => x.ur.UserId == userId && x.r.Name == role && x.r.IsActive)
                    .AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role {Role} for user {UserId}", role, userId);
                return false;
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            try
            {
                // Get direct permissions
                var directPermissions = await _context.UserPermissions
                    .Join(_context.Permissions, up => up.PermissionId, p => p.Id, (up, p) => new { up, p })
                    .Where(x => x.up.UserId == userId && x.up.IsGranted)
                    .Select(x => x.p.Name)
                    .ToListAsync();

                // Get role-based permissions
                var rolePermissions = await _context.UserRoles
                    .Join(_context.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => new { ur, rp })
                    .Join(_context.Permissions, x => x.rp.PermissionId, p => p.Id, (x, p) => new { x.ur, x.rp, p })
                    .Where(x => x.ur.UserId == userId)
                    .Select(x => x.p.Name)
                    .ToListAsync();

                // Merge and remove duplicates
                var allPermissions = directPermissions.Concat(rolePermissions).Distinct().ToList();

                // Remove any explicitly denied permissions
                var deniedPermissions = await _context.UserPermissions
                    .Join(_context.Permissions, up => up.PermissionId, p => p.Id, (up, p) => new { up, p })
                    .Where(x => x.up.UserId == userId && !x.up.IsGranted)
                    .Select(x => x.p.Name)
                    .ToListAsync();

                return allPermissions.Except(deniedPermissions).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            try
            {
                return await _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                    .Where(x => x.ur.UserId == userId && x.r.IsActive)
                    .Select(x => x.r.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, string roleName, int assignedByUserId)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive);
                if (role == null)
                {
                    _logger.LogWarning("Role {RoleName} not found", roleName);
                    return false;
                }

                // Check if user already has this role
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

                if (existingUserRole != null)
                {
                    _logger.LogInformation("User {UserId} already has role {RoleName}", userId, roleName);
                    return true;
                }

                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    AssignedByUserId = assignedByUserId
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Role {RoleName} assigned to user {UserId} by user {AssignedBy}", 
                    roleName, userId, assignedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", roleName, userId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, string roleName, int removedByUserId)
        {
            try
            {
                var userRole = await _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                    .Where(x => x.ur.UserId == userId && x.r.Name == roleName)
                    .Select(x => x.ur)
                    .FirstOrDefaultAsync();

                if (userRole == null)
                {
                    _logger.LogInformation("User {UserId} does not have role {RoleName}", userId, roleName);
                    return true;
                }

                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Role {RoleName} removed from user {UserId} by user {RemovedBy}", 
                    roleName, userId, removedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
                return false;
            }
        }

        public async Task<bool> GrantPermissionToUserAsync(int userId, string permission, int grantedByUserId)
        {
            try
            {
                var perm = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permission);
                if (perm == null)
                {
                    _logger.LogWarning("Permission {Permission} not found", permission);
                    return false;
                }

                var existingPermission = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == perm.Id);

                if (existingPermission != null)
                {
                    existingPermission.IsGranted = true;
                    existingPermission.AssignedByUserId = grantedByUserId;
                    existingPermission.AssignedAt = DateTime.UtcNow;
                }
                else
                {
                    var userPermission = new UserPermission
                    {
                        UserId = userId,
                        PermissionId = perm.Id,
                        IsGranted = true,
                        AssignedByUserId = grantedByUserId
                    };

                    _context.UserPermissions.Add(userPermission);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Permission {Permission} granted to user {UserId} by user {GrantedBy}", 
                    permission, userId, grantedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission {Permission} to user {UserId}", permission, userId);
                return false;
            }
        }

        public async Task<bool> RevokePermissionFromUserAsync(int userId, string permission, int revokedByUserId)
        {
            try
            {
                var perm = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permission);
                if (perm == null)
                {
                    _logger.LogWarning("Permission {Permission} not found", permission);
                    return false;
                }

                var existingPermission = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == perm.Id);

                if (existingPermission != null)
                {
                    existingPermission.IsGranted = false;
                    existingPermission.AssignedByUserId = revokedByUserId;
                    existingPermission.AssignedAt = DateTime.UtcNow;
                }
                else
                {
                    var userPermission = new UserPermission
                    {
                        UserId = userId,
                        PermissionId = perm.Id,
                        IsGranted = false,
                        AssignedByUserId = revokedByUserId
                    };

                    _context.UserPermissions.Add(userPermission);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Permission {Permission} revoked from user {UserId} by user {RevokedBy}", 
                    permission, userId, revokedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission {Permission} from user {UserId}", permission, userId);
                return false;
            }
        }

        public async Task InitializeSystemPermissionsAsync()
        {
            try
            {
                var systemPermissions = new[]
                {
                    new { Name = "SYSTEM_ADMIN", Description = "Full system administration access", Category = "System", Path = "/admin/*" },
                    new { Name = "SERVICE_MANAGEMENT", Description = "Install/manage Windows services", Category = "Services", Path = "/admin/services/*" },
                    new { Name = "USER_MANAGEMENT", Description = "Manage user accounts and permissions", Category = "Users", Path = "/admin/users/*" },
                    new { Name = "CONFIG_MANAGEMENT", Description = "Manage system configuration", Category = "Configuration", Path = "/admin/config/*" },
                    new { Name = "LOG_MANAGEMENT", Description = "View and manage system logs", Category = "Logs", Path = "/admin/logs/*" },
                    new { Name = "BETA_SCENARIOS", Description = "Access beta scenarios", Category = "Pages", Path = "/betascenarios" },
                    new { Name = "SYSTEM_CONFIGURATION", Description = "Access system configuration", Category = "Pages", Path = "/admin/systemconfiguration" },
                    new { Name = "SYSTEM_LOGS", Description = "Access system logs", Category = "Pages", Path = "/admin/systemlogs" },
                    new { Name = "API_ACCESS", Description = "Access API endpoints", Category = "API", Path = "/api/*" },
                    new { Name = "GENERATE_SCENARIOS", Description = "Generate new scenarios", Category = "API", Path = "/api/generations" },
                    new { Name = "TEST_SCENARIOS", Description = "Test scenarios against APIs", Category = "API", Path = "/api/*/test" }
                };

                foreach (var perm in systemPermissions)
                {
                    var existingPermission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.Name == perm.Name);

                    if (existingPermission == null)
                    {
                        var permission = new Permission
                        {
                            Name = perm.Name,
                            Description = perm.Description,
                            Category = perm.Category,
                            ResourcePath = perm.Path,
                            IsSystemPermission = true
                        };

                        _context.Permissions.Add(permission);
                    }
                }

                // Create system roles
                var systemRoles = new[]
                {
                    new { Name = "SuperAdmin", Description = "Super Administrator with full system access" },
                    new { Name = "Admin", Description = "Administrator with elevated privileges" },
                    new { Name = "User", Description = "Standard user access" },
                    new { Name = "ServiceManager", Description = "Can manage Windows services" },
                    new { Name = "ConfigManager", Description = "Can manage system configuration" }
                };

                foreach (var role in systemRoles)
                {
                    var existingRole = await _context.Roles
                        .FirstOrDefaultAsync(r => r.Name == role.Name);

                    if (existingRole == null)
                    {
                        var newRole = new Role
                        {
                            Name = role.Name,
                            Description = role.Description,
                            IsSystemRole = true
                        };

                        _context.Roles.Add(newRole);
                    }
                }

                await _context.SaveChangesAsync();

                // Assign permissions to roles
                await AssignPermissionsToRolesAsync();

                _logger.LogInformation("System permissions and roles initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing system permissions");
                throw;
            }
        }

        private async Task AssignPermissionsToRolesAsync()
        {
            var superAdminRole = await _context.Roles.FirstAsync(r => r.Name == "SuperAdmin");
            var adminRole = await _context.Roles.FirstAsync(r => r.Name == "Admin");
            var serviceManagerRole = await _context.Roles.FirstAsync(r => r.Name == "ServiceManager");
            var configManagerRole = await _context.Roles.FirstAsync(r => r.Name == "ConfigManager");

            var allPermissions = await _context.Permissions.ToListAsync();

            // SuperAdmin gets all permissions
            foreach (var permission in allPermissions)
            {
                if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == superAdminRole.Id && rp.PermissionId == permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = superAdminRole.Id,
                        PermissionId = permission.Id,
                        AssignedByUserId = 1 // System user
                    });
                }
            }

            // Admin gets most permissions except SYSTEM_ADMIN
            var adminPermissions = allPermissions.Where(p => p.Name != "SYSTEM_ADMIN").ToList();
            foreach (var permission in adminPermissions)
            {
                if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id,
                        AssignedByUserId = 1 // System user
                    });
                }
            }

            // Service Manager gets service-related permissions
            var servicePermissions = allPermissions.Where(p => 
                p.Name == "SERVICE_MANAGEMENT" || 
                p.Name == "LOG_MANAGEMENT" || 
                p.Name == "BETA_SCENARIOS").ToList();
            
            foreach (var permission in servicePermissions)
            {
                if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == serviceManagerRole.Id && rp.PermissionId == permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = serviceManagerRole.Id,
                        PermissionId = permission.Id,
                        AssignedByUserId = 1 // System user
                    });
                }
            }

            // Config Manager gets configuration-related permissions
            var configPermissions = allPermissions.Where(p => 
                p.Name == "CONFIG_MANAGEMENT" || 
                p.Name == "SYSTEM_CONFIGURATION" || 
                p.Name == "BETA_SCENARIOS").ToList();
            
            foreach (var permission in configPermissions)
            {
                if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == configManagerRole.Id && rp.PermissionId == permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = configManagerRole.Id,
                        PermissionId = permission.Id,
                        AssignedByUserId = 1 // System user
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> CanUserPerformServiceOperationsAsync(int userId)
        {
            return await HasPermissionAsync(userId, "SERVICE_MANAGEMENT") ||
                   await HasPermissionAsync(userId, "SYSTEM_ADMIN");
        }

        public async Task<bool> CanUserManageUsersAsync(int userId)
        {
            return await HasPermissionAsync(userId, "USER_MANAGEMENT") ||
                   await HasPermissionAsync(userId, "SYSTEM_ADMIN");
        }

        public async Task LogAdminActionAsync(int adminUserId, string action, string targetType, int? targetId = null,
            string? targetName = null, object? oldValue = null, object? newValue = null, string? description = null,
            string ipAddress = "", string userAgent = "", bool isSuccessful = true, string? errorMessage = null)
        {
            try
            {
                var log = new AdminActionLog
                {
                    AdminUserId = adminUserId,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId,
                    TargetName = targetName,
                    OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                    NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                    Description = description,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsSuccessful = isSuccessful,
                    ErrorMessage = errorMessage
                };

                _context.AdminActionLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging admin action {Action} for user {UserId}", action, adminUserId);
            }
        }
    }
}
