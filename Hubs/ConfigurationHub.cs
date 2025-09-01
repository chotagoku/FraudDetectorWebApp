using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FraudDetectorWebApp.Hubs
{
    [Authorize]
    public class ConfigurationHub : Hub
    {
        private readonly ILogger<ConfigurationHub> _logger;

        public ConfigurationHub(ILogger<ConfigurationHub> logger) { _logger = logger; }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Configuration client connected: {ConnectionId}", Context.ConnectionId);

            // Join the administrators group for configuration updates
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Administrators");
                _logger.LogInformation("Admin user added to Administrators group: {ConnectionId}", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Configuration client disconnected: {ConnectionId}", Context.ConnectionId);

            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Administrators");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Method to broadcast configuration updates to all connected clients
        public async Task NotifyConfigurationUpdated(string configurationKey, object newValue, string updatedBy)
        {
            await Clients.Group("Administrators")
                .SendAsync("ConfigurationUpdated", configurationKey, newValue, updatedBy);
        }

        // Method to broadcast service status changes
        public async Task NotifyServiceStatusChanged(string serviceName, bool isRunning)
        { await Clients.Group("Administrators").SendAsync("ServiceStatusChanged", serviceName, isRunning); }

        // Method to broadcast system health updates
        public async Task NotifySystemHealthChanged(object healthData)
        { await Clients.Group("Administrators").SendAsync("SystemHealthChanged", healthData); }

        // Method for clients to request current system health
        public async Task RequestSystemHealth()
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                // This would typically call the health service and return current status
                await Clients.Caller.SendAsync("SystemHealthRequested");
            }
        }

        // Method for clients to join specific configuration monitoring groups
        public async Task JoinConfigurationGroup(string configurationKey)
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Config_{configurationKey}");
                _logger.LogInformation(
                    "Client {ConnectionId} joined configuration group: {ConfigKey}",
                    Context.ConnectionId,
                    configurationKey);
            }
        }

        // Method for clients to leave specific configuration monitoring groups
        public async Task LeaveConfigurationGroup(string configurationKey)
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Config_{configurationKey}");
                _logger.LogInformation(
                    "Client {ConnectionId} left configuration group: {ConfigKey}",
                    Context.ConnectionId,
                    configurationKey);
            }
        }
    }
}
