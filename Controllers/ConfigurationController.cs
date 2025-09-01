using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using FraudDetectorWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ApiRequestService _apiRequestService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            ApplicationDbContext context,
            ApiRequestService apiRequestService,
            ILogger<ConfigurationController> logger)
        {
            _context = context;
            _apiRequestService = apiRequestService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetConfigurations()
        {
            return await _context.ApiConfigurations
                .OrderByDescending(c => c.CreatedAt)
                .Select(
                    c => new
                    {
                        c.Id,
                        c.Name,
                        c.ApiEndpoint,
                        c.RequestTemplate,
                        c.BearerToken,
                        c.DelayBetweenRequests,
                        c.MaxIterations,
                        c.IsActive,
                        c.CreatedAt,
                        c.UpdatedAt,
                        RequestLogsCount = c.RequestLogs.Count(),
                        LastRequestTime = c.RequestLogs
                            .OrderByDescending(r => r.RequestTimestamp)
                            .Select(r => r.RequestTimestamp)
                            .FirstOrDefault()
                    })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetConfiguration(int id)
        {
            var config = await _context.ApiConfigurations
                .Where(c => c.Id == id)
                .Select(
                    c => new
                    {
                        c.Id,
                        c.Name,
                        c.ApiEndpoint,
                        c.RequestTemplate,
                        c.BearerToken,
                        c.DelayBetweenRequests,
                        c.MaxIterations,
                        c.IsActive,
                        c.CreatedAt,
                        c.UpdatedAt,
                        RequestLogsCount = c.RequestLogs.Count(),
                        LastRequestTime = c.RequestLogs
                            .OrderByDescending(r => r.RequestTimestamp)
                            .Select(r => r.RequestTimestamp)
                            .FirstOrDefault()
                    })
                .FirstOrDefaultAsync();

            if (config == null)
                return NotFound();

            return config;
        }

        [HttpPost]
        public async Task<ActionResult<ApiConfiguration>> CreateConfiguration(ApiConfiguration configuration)
        {
            configuration.CreatedAt = DateTime.UtcNow;
            _context.ApiConfigurations.Add(configuration);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConfiguration), new { id = configuration.Id }, configuration);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConfiguration(int id, ApiConfiguration configuration)
        {
            if (id != configuration.Id)
                return BadRequest();

            var existingConfig = await _context.ApiConfigurations.FindAsync(id);
            if (existingConfig == null)
                return NotFound();

            existingConfig.Name = configuration.Name;
            existingConfig.ApiEndpoint = configuration.ApiEndpoint;
            existingConfig.RequestTemplate = configuration.RequestTemplate;
            existingConfig.BearerToken = configuration.BearerToken;
            existingConfig.DelayBetweenRequests = configuration.DelayBetweenRequests;
            existingConfig.MaxIterations = configuration.MaxIterations;
            existingConfig.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConfiguration(int id)
        {
            var configuration = await _context.ApiConfigurations.FindAsync(id);
            if (configuration == null)
                return NotFound();

            _context.ApiConfigurations.Remove(configuration);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartConfiguration(int id)
        {
            var configuration = await _context.ApiConfigurations.FindAsync(id);
            if (configuration == null)
                return NotFound();

            configuration.IsActive = true;
            await _context.SaveChangesAsync();

            if (!_apiRequestService.IsRunning)
            {
                await _apiRequestService.StartLoop();
            }

            return Ok(new { message = "Configuration started successfully" });
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopConfiguration(int id)
        {
            var configuration = await _context.ApiConfigurations.FindAsync(id);
            if (configuration == null)
                return NotFound();

            configuration.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Configuration stopped successfully" });
        }

        [HttpPost("start-all")]
        public async Task<IActionResult> StartAllConfigurations()
        {
            await _apiRequestService.StartLoop();
            return Ok(new { message = "All active configurations started successfully" });
        }

        [HttpPost("stop-all")]
        public async Task<IActionResult> StopAllConfigurations()
        {
            await _apiRequestService.StopLoop();

            // Also mark all configurations as inactive
            var activeConfigs = await _context.ApiConfigurations.Where(c => c.IsActive).ToListAsync();

            foreach (var config in activeConfigs)
            {
                config.IsActive = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "All configurations stopped successfully" });
        }

        [HttpGet("status")]
        public IActionResult GetStatus() { return Ok(new { isRunning = _apiRequestService.IsRunning }); }
    }
}
