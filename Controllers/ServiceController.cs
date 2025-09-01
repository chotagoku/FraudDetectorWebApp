using FraudDetectorWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly WindowsServiceInstaller _serviceInstaller;
        private readonly ILogger<ServiceController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ServiceController(
            WindowsServiceInstaller serviceInstaller,
            ILogger<ServiceController> logger,
            IWebHostEnvironment environment)
        {
            _serviceInstaller = serviceInstaller;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet("status")]
        public ActionResult<object> GetServiceStatus()
        {
            try
            {
                var status = _serviceInstaller.GetServiceStatus();
                var isAdmin = _serviceInstaller.IsRunningAsAdministrator();
                var executablePath = GetExecutablePath();

                return Ok(new
                {
                    service = status,
                    system = new
                    {
                        isAdministrator = isAdmin,
                        executablePath = executablePath,
                        environment = _environment.EnvironmentName,
                        applicationVersion = GetApplicationVersion()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service status");
                return StatusCode(500, new { message = "Error getting service status", error = ex.Message });
            }
        }

        [HttpPost("install")]
        public async Task<ActionResult<object>> InstallService()
        {
            try
            {
                var executablePath = GetExecutablePath();
                if (string.IsNullOrEmpty(executablePath))
                {
                    return BadRequest(new { message = "Could not determine executable path" });
                }

                var result = await _serviceInstaller.InstallServiceAsync(executablePath, "--urls=http://localhost:5207");
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        output = result.Output
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        requiresElevation = result.RequiresElevation,
                        isAlreadyInstalled = result.IsAlreadyInstalled,
                        output = result.Output
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing service");
                return StatusCode(500, new { message = "Error installing service", error = ex.Message });
            }
        }

        [HttpPost("uninstall")]
        public async Task<ActionResult<object>> UninstallService()
        {
            try
            {
                var result = await _serviceInstaller.UninstallServiceAsync();
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        output = result.Output
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        requiresElevation = result.RequiresElevation,
                        output = result.Output
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uninstalling service");
                return StatusCode(500, new { message = "Error uninstalling service", error = ex.Message });
            }
        }

        [HttpPost("start")]
        public async Task<ActionResult<object>> StartService()
        {
            try
            {
                var result = await _serviceInstaller.StartServiceAsync();
                
                return Ok(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting service");
                return StatusCode(500, new { message = "Error starting service", error = ex.Message });
            }
        }

        [HttpPost("stop")]
        public async Task<ActionResult<object>> StopService()
        {
            try
            {
                var result = await _serviceInstaller.StopServiceAsync();
                
                return Ok(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping service");
                return StatusCode(500, new { message = "Error stopping service", error = ex.Message });
            }
        }

        [HttpPost("restart")]
        public async Task<ActionResult<object>> RestartService()
        {
            try
            {
                // Stop first
                var stopResult = await _serviceInstaller.StopServiceAsync();
                if (!stopResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Failed to stop service: {stopResult.Message}"
                    });
                }

                // Wait a bit
                await Task.Delay(2000);

                // Start again
                var startResult = await _serviceInstaller.StartServiceAsync();
                
                return Ok(new
                {
                    success = startResult.Success,
                    message = startResult.Success 
                        ? "Service restarted successfully"
                        : $"Service stopped but failed to start: {startResult.Message}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting service");
                return StatusCode(500, new { message = "Error restarting service", error = ex.Message });
            }
        }

        [HttpGet("logs")]
        public ActionResult<object> GetServiceLogs()
        {
            try
            {
                // Get recent application logs that would be relevant to the service
                var logPath = Path.Combine(_environment.ContentRootPath, "Logs");
                var logs = new List<object>();

                if (Directory.Exists(logPath))
                {
                    var logFiles = Directory.GetFiles(logPath, "*.log")
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(5);

                    foreach (var file in logFiles)
                    {
                        try
                        {
                            var lines = System.IO.File.ReadAllLines(file).TakeLast(50);
                            logs.Add(new
                            {
                                fileName = Path.GetFileName(file),
                                lastModified = new FileInfo(file).LastWriteTime,
                                lines = lines.ToArray()
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not read log file: {File}", file);
                        }
                    }
                }

                return Ok(new
                {
                    logPath = logPath,
                    logs = logs,
                    message = logs.Count > 0 ? $"Found {logs.Count} log files" : "No log files found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service logs");
                return StatusCode(500, new { message = "Error getting service logs", error = ex.Message });
            }
        }

        private string GetExecutablePath()
        {
            try
            {
                // For published applications, use the executable
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var directory = Path.GetDirectoryName(assemblyLocation);
                
                if (directory != null)
                {
                    var exePath = Path.Combine(directory, "FraudDetectorWebApp.exe");
                    if (System.IO.File.Exists(exePath))
                    {
                        return exePath;
                    }
                }

                // For development, use dotnet run command
                var projectPath = _environment.ContentRootPath;
                return $"dotnet run --project \"{projectPath}\"";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining executable path");
                return string.Empty;
            }
        }

        private string GetApplicationVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
