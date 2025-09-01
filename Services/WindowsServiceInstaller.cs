using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;

namespace FraudDetectorWebApp.Services
{
    public class WindowsServiceInstaller
    {
        private readonly ILogger<WindowsServiceInstaller> _logger;
        private readonly string _serviceName = "FraudDetectorWebApp";
        private readonly string _displayName = "Fraud Detector Web Application";
        private readonly string _description = "Fraud Detector Pro - API Testing and Fraud Detection Service";

        public WindowsServiceInstaller(ILogger<WindowsServiceInstaller> logger)
        {
            _logger = logger;
        }

        public async Task<ServiceInstallResult> InstallServiceAsync(string executablePath, string arguments = "")
        {
            try
            {
                if (!IsRunningAsAdministrator())
                {
                    return new ServiceInstallResult
                    {
                        Success = false,
                        Message = "Administrator privileges required to install Windows Service",
                        RequiresElevation = true
                    };
                }

                if (IsServiceInstalled())
                {
                    return new ServiceInstallResult
                    {
                        Success = false,
                        Message = "Service is already installed",
                        IsAlreadyInstalled = true
                    };
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"create \"{_serviceName}\" binPath=\"{executablePath} {arguments}\" DisplayName=\"{_displayName}\" start=auto",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    if (process.ExitCode == 0)
                    {
                        // Set service description
                        await SetServiceDescriptionAsync();
                        
                        _logger.LogInformation("Windows Service installed successfully");
                        return new ServiceInstallResult
                        {
                            Success = true,
                            Message = "Service installed successfully",
                            Output = output
                        };
                    }
                    else
                    {
                        _logger.LogError("Failed to install service: {Error}", error);
                        return new ServiceInstallResult
                        {
                            Success = false,
                            Message = $"Failed to install service: {error}",
                            Output = output
                        };
                    }
                }

                return new ServiceInstallResult
                {
                    Success = false,
                    Message = "Failed to start installation process"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during service installation");
                return new ServiceInstallResult
                {
                    Success = false,
                    Message = $"Exception during installation: {ex.Message}"
                };
            }
        }

        public async Task<ServiceInstallResult> UninstallServiceAsync()
        {
            try
            {
                if (!IsRunningAsAdministrator())
                {
                    return new ServiceInstallResult
                    {
                        Success = false,
                        Message = "Administrator privileges required to uninstall Windows Service",
                        RequiresElevation = true
                    };
                }

                if (!IsServiceInstalled())
                {
                    return new ServiceInstallResult
                    {
                        Success = false,
                        Message = "Service is not installed"
                    };
                }

                // Stop the service first if it's running
                await StopServiceAsync();

                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"delete \"{_serviceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation("Windows Service uninstalled successfully");
                        return new ServiceInstallResult
                        {
                            Success = true,
                            Message = "Service uninstalled successfully",
                            Output = output
                        };
                    }
                    else
                    {
                        _logger.LogError("Failed to uninstall service: {Error}", error);
                        return new ServiceInstallResult
                        {
                            Success = false,
                            Message = $"Failed to uninstall service: {error}",
                            Output = output
                        };
                    }
                }

                return new ServiceInstallResult
                {
                    Success = false,
                    Message = "Failed to start uninstallation process"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during service uninstallation");
                return new ServiceInstallResult
                {
                    Success = false,
                    Message = $"Exception during uninstallation: {ex.Message}"
                };
            }
        }

        public async Task<ServiceControlResult> StartServiceAsync()
        {
            try
            {
                if (!IsServiceInstalled())
                {
                    return new ServiceControlResult
                    {
                        Success = false,
                        Message = "Service is not installed"
                    };
                }

                using var service = new ServiceController(_serviceName);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    return new ServiceControlResult
                    {
                        Success = true,
                        Message = "Service is already running"
                    };
                }

                service.Start();
                await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));

                _logger.LogInformation("Windows Service started successfully");
                return new ServiceControlResult
                {
                    Success = true,
                    Message = "Service started successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception starting service");
                return new ServiceControlResult
                {
                    Success = false,
                    Message = $"Failed to start service: {ex.Message}"
                };
            }
        }

        public async Task<ServiceControlResult> StopServiceAsync()
        {
            try
            {
                if (!IsServiceInstalled())
                {
                    return new ServiceControlResult
                    {
                        Success = false,
                        Message = "Service is not installed"
                    };
                }

                using var service = new ServiceController(_serviceName);
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    return new ServiceControlResult
                    {
                        Success = true,
                        Message = "Service is already stopped"
                    };
                }

                service.Stop();
                await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));

                _logger.LogInformation("Windows Service stopped successfully");
                return new ServiceControlResult
                {
                    Success = true,
                    Message = "Service stopped successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception stopping service");
                return new ServiceControlResult
                {
                    Success = false,
                    Message = $"Failed to stop service: {ex.Message}"
                };
            }
        }

        public ServiceStatus GetServiceStatus()
        {
            try
            {
                if (!IsServiceInstalled())
                {
                    return new ServiceStatus
                    {
                        IsInstalled = false,
                        Status = "Not Installed",
                        StartType = "Unknown"
                    };
                }

                using var service = new ServiceController(_serviceName);
                service.Refresh();

                return new ServiceStatus
                {
                    IsInstalled = true,
                    Status = service.Status.ToString(),
                    StartType = service.StartType.ToString(),
                    CanStart = service.Status == ServiceControllerStatus.Stopped,
                    CanStop = service.Status == ServiceControllerStatus.Running,
                    CanPause = service.CanPauseAndContinue && service.Status == ServiceControllerStatus.Running,
                    DisplayName = service.DisplayName,
                    ServiceName = service.ServiceName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service status");
                return new ServiceStatus
                {
                    IsInstalled = false,
                    Status = "Error",
                    StartType = "Unknown",
                    ErrorMessage = ex.Message
                };
            }
        }

        public bool IsServiceInstalled()
        {
            try
            {
                using var service = new ServiceController(_serviceName);
                var _ = service.Status; // This will throw if service doesn't exist
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private async Task SetServiceDescriptionAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"description \"{_serviceName}\" \"{_description}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set service description");
            }
        }
    }

    public class ServiceInstallResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public bool RequiresElevation { get; set; }
        public bool IsAlreadyInstalled { get; set; }
    }

    public class ServiceControlResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ServiceStatus
    {
        public bool IsInstalled { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StartType { get; set; } = string.Empty;
        public bool CanStart { get; set; }
        public bool CanStop { get; set; }
        public bool CanPause { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
