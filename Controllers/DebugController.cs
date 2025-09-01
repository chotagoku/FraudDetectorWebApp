using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly DatabaseSeeder _seeder;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DebugController> _logger;
        private readonly IWebHostEnvironment _env;

        public DebugController(
            DatabaseSeeder seeder, 
            ApplicationDbContext context,
            ILogger<DebugController> logger, 
            IWebHostEnvironment env)
        {
            _seeder = seeder;
            _context = context;
            _logger = logger;
            _env = env;
        }

        [HttpPost("create-admin-user")]
        public async Task<IActionResult> CreateAdminUser()
        {
            // Only allow in development environment
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { message = "This endpoint is only available in development environment" });
            }

            try
            {
                await _seeder.ManuallyCreateAdminUserAsync();
                return Ok(new { 
                    success = true, 
                    message = "Admin user created/updated successfully",
                    credentials = new {
                        email = "admin@test.com",
                        password = "password123",
                        role = "Admin"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create admin user");
                return StatusCode(500, new { message = "Failed to create admin user", error = ex.Message });
            }
        }

        [HttpPost("update-user-role/{email}")]
        public async Task<IActionResult> UpdateUserRole(string email, [FromQuery] string role = "Admin")
        {
            // Only allow in development environment  
            if (!_env.IsDevelopment())
            {
                return BadRequest(new { message = "This endpoint is only available in development environment" });
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return NotFound(new { message = $"User with email {email} not found" });
                }

                var oldRole = user.Role;
                user.Role = role;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user {Email} role from {OldRole} to {NewRole}", email, oldRole, role);

                return Ok(new
                {
                    success = true,
                    message = $"User {email} role updated from {oldRole} to {role}",
                    user = new
                    {
                        email = user.Email,
                        name = $"{user.FirstName} {user.LastName}",
                        oldRole = oldRole,
                        newRole = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user role for {Email}", email);
                return StatusCode(500, new { message = "Failed to update user role", error = ex.Message });
            }
        }

        [HttpGet("system-info")]
        public IActionResult GetSystemInfo()
        {
            // Allow in any environment since this is just info
            return Ok(new
            {
                environment = _env.EnvironmentName,
                isDevelopment = _env.IsDevelopment(),
                machineName = Environment.MachineName,
                currentTime = DateTime.UtcNow,
                adminCredentials = new {
                    email = "admin@test.com",
                    password = "password123",
                    note = "Use POST /api/debug/create-admin-user to create/update admin user"
                },
                availableEndpoints = new {
                    updateRole = "POST /api/debug/update-user-role/{email}?role=Admin",
                    createAdmin = "POST /api/debug/create-admin-user"
                }
            });
        }
    }
}
