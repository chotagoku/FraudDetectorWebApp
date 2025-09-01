using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using FraudDetectorWebApp.Repositories;
using FraudDetectorWebApp.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserRepository userRepository, ILogger<AccountController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto request)
        {
            try
            {
                // Validate request
                if(string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required." });
                }

                if(request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new { message = "Passwords do not match." });
                }

                // Check if user already exists
                if(await _userRepository.EmailExistsAsync(request.Email))
                {
                    return BadRequest(new { message = "A user with this email already exists." });
                }

                // Hash password
                var passwordHash = HashPassword(request.Password);

                // Create new user
                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email.ToLower(),
                    PasswordHash = passwordHash,
                    Phone = request.Phone,
                    Company = request.Company,
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.CreateAsync(user);

                _logger.LogInformation("New user registered: {Email}", user.Email);

                return Ok(
                    new ApiResponseDto<object>
                    {
                        Success = true,
                        Message = "Registration successful! Please login with your credentials.",
                        Data = null
                    });
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { message = "An error occurred during registration. Please try again." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            try
            {
                // Validate request
                if(string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required." });
                }

                // Find user
                var user = await _userRepository.GetByEmailAsync(request.Email);

                if(user == null || !VerifyPassword(request.Password, user.PasswordHash))
                {
                    return BadRequest(new { message = "Invalid email or password." });
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync("Cookies", claimsPrincipal);

                _logger.LogInformation("User logged in: {Email}", user.Email);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.FullName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Company = user.Company,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return Ok(
                    new ApiResponseDto<UserDto>
                    {
                        Success = true,
                        Message = "Login successful! Welcome back.",
                        Data = userDto
                    });
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new { message = "An error occurred during login. Please try again." });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync("Cookies");
                return Ok(new ApiResponseDto<object> { Success = true, Message = "Logout successful!", Data = null });
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _userRepository.GetByIdAsync(userId);

                if(user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.FullName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Company = user.Company,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return Ok(
                    new ApiResponseDto<UserDto>
                    {
                        Success = true,
                        Message = "User information retrieved successfully",
                        Data = userDto
                    });
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An error occurred while getting user information." });
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = password + "FraudDetectorSalt2025"; // Use a proper salt in production
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashToCheck = HashPassword(password);
            return hashToCheck == hashedPassword;
        }
    }
}
