using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.DTOs;
using FraudDetectorWebApp.Hubs;
using FraudDetectorWebApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FraudDetectorWebApp.Services
{
    public interface IConfigurationService
    {
        Task<T?> GetConfigurationValueAsync<T>(string key, T? defaultValue = default);
        Task<string?> GetConfigurationValueAsync(string key, string? defaultValue = null);
        Task<bool> SetConfigurationValueAsync(string key, object value, string? updatedBy = null);
        Task<Dictionary<string, object>> GetAllConfigurationsAsync();
        Task<Dictionary<string, object>> GetConfigurationsByCategoryAsync(string category);
        Task<Dictionary<string, object>> GetConfigurationsBySectionAsync(string section);
        Task<bool> BulkUpdateConfigurationsAsync(Dictionary<string, object> configurations, string? updatedBy = null);
        Task<ConfigurationValidationResultDto> ValidateConfigurationAsync(string key, string value);
        Task NotifyServicesOfConfigurationChangeAsync(string key, object oldValue, object newValue);
        Task<List<SystemConfiguration>> SearchConfigurationsAsync(ConfigurationSearchDto search);
        Task<ConfigurationImportResultDto> ImportConfigurationsAsync(ConfigurationImportDto import);
        Task<string> ExportConfigurationsAsync(ConfigurationExportDto export);
        Task ResetConfigurationToDefaultAsync(string key, string? updatedBy = null);
        Task<bool> ConfigurationExistsAsync(string key);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<ConfigurationHub> _hubContext;
        private readonly Dictionary<string, object> _cache = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        // Event for configuration changes
        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public ConfigurationService(
            ApplicationDbContext context,
            ILogger<ConfigurationService> logger,
            IServiceProvider serviceProvider,
            IHubContext<ConfigurationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        public async Task<T?> GetConfigurationValueAsync<T>(string key, T? defaultValue = default)
        {
            try
            {
                var stringValue = await GetConfigurationValueAsync(key);
                if (string.IsNullOrEmpty(stringValue))
                    return defaultValue;

                return ParseValue<T>(stringValue, defaultValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration value for key: {Key}", key);
                return defaultValue;
            }
        }

        public async Task<string?> GetConfigurationValueAsync(string key, string? defaultValue = null)
        {
            try
            {
                await RefreshCacheIfNeeded();

                if (_cache.TryGetValue(key, out var cachedValue))
                {
                    return cachedValue?.ToString();
                }

                var config = await _context.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == key);

                var value = config?.Value ?? defaultValue;

                // Cache the value
                await _cacheLock.WaitAsync();
                try
                {
                    _cache[key] = value ?? string.Empty;
                }
                finally
                {
                    _cacheLock.Release();
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration value for key: {Key}", key);
                return defaultValue;
            }
        }

        public async Task<bool> SetConfigurationValueAsync(string key, object value, string? updatedBy = null)
        {
            try
            {
                var config = await _context.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == key);

                if (config == null)
                {
                    _logger.LogWarning("Configuration key not found: {Key}", key);
                    return false;
                }

                if (config.IsReadOnly)
                {
                    _logger.LogWarning("Attempted to modify read-only configuration: {Key}", key);
                    return false;
                }

                var oldValue = config.Value;
                var newValue = value.ToString() ?? string.Empty;

                // Validate the new value
                var validation = await ValidateConfigurationAsync(key, newValue);
                if (!validation.IsValid)
                {
                    _logger.LogWarning("Validation failed for configuration {Key}: {Errors}", key, string.Join(", ", validation.Errors));
                    return false;
                }

                config.Value = newValue;
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                // Clear cache
                await _cacheLock.WaitAsync();
                try
                {
                    _cache.Remove(key);
                    _lastCacheUpdate = DateTime.MinValue;
                }
                finally
                {
                    _cacheLock.Release();
                }

                // Notify services of the change
                await NotifyServicesOfConfigurationChangeAsync(key, oldValue, newValue);

                // Trigger event
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, oldValue, newValue, updatedBy));

                // Send real-time notification via SignalR
                await _hubContext.Clients.Group("Administrators")
                    .SendAsync("ConfigurationUpdated", key, newValue, updatedBy ?? "System");

                _logger.LogInformation("Configuration updated: {Key} = {Value} by {UpdatedBy}", key, newValue, updatedBy ?? "System");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting configuration value for key: {Key}", key);
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetAllConfigurationsAsync()
        {
            try
            {
                var configs = await _context.SystemConfigurations
                    .OrderBy(c => c.Section)
                    .ThenBy(c => c.DisplayOrder)
                    .ToListAsync();

                return configs.ToDictionary(c => c.Key, c => ParseValue(c.Value, c.DataType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all configurations");
                return new Dictionary<string, object>();
            }
        }

        public async Task<Dictionary<string, object>> GetConfigurationsByCategoryAsync(string category)
        {
            try
            {
                var configs = await _context.SystemConfigurations
                    .Where(c => c.Category == category)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                return configs.ToDictionary(c => c.Key, c => ParseValue(c.Value, c.DataType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configurations for category: {Category}", category);
                return new Dictionary<string, object>();
            }
        }

        public async Task<Dictionary<string, object>> GetConfigurationsBySectionAsync(string section)
        {
            try
            {
                var configs = await _context.SystemConfigurations
                    .Where(c => c.Section == section)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                return configs.ToDictionary(c => c.Key, c => ParseValue(c.Value, c.DataType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configurations for section: {Section}", section);
                return new Dictionary<string, object>();
            }
        }

        public async Task<bool> BulkUpdateConfigurationsAsync(Dictionary<string, object> configurations, string? updatedBy = null)
        {
            try
            {
                var updated = 0;
                var errors = new List<string>();

                foreach (var kvp in configurations)
                {
                    var success = await SetConfigurationValueAsync(kvp.Key, kvp.Value, updatedBy);
                    if (success)
                    {
                        updated++;
                    }
                    else
                    {
                        errors.Add($"Failed to update {kvp.Key}");
                    }
                }

                _logger.LogInformation("Bulk update completed: {Updated}/{Total} configurations updated by {UpdatedBy}",
                    updated, configurations.Count, updatedBy ?? "System");

                if (errors.Any())
                {
                    _logger.LogWarning("Bulk update had errors: {Errors}", string.Join(", ", errors));
                }

                return updated > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk configuration update");
                return false;
            }
        }

        public async Task<ConfigurationValidationResultDto> ValidateConfigurationAsync(string key, string value)
        {
            var result = new ConfigurationValidationResultDto { IsValid = true };

            try
            {
                var config = await _context.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == key);

                if (config == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Configuration key '{key}' not found");
                    return result;
                }

                // Validate data type
                var parsedValue = TryParseValue(value, config.DataType);
                if (parsedValue == null && !string.IsNullOrEmpty(value))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Value '{value}' is not a valid {config.DataType}");
                    return result;
                }

                result.ParsedValue = parsedValue;

                // Validate allowed values
                if (!string.IsNullOrEmpty(config.AllowedValues))
                {
                    try
                    {
                        var allowedValues = JsonSerializer.Deserialize<string[]>(config.AllowedValues);
                        if (allowedValues != null && !allowedValues.Contains(value))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Value must be one of: {string.Join(", ", allowedValues)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing allowed values for {Key}", key);
                    }
                }

                // Validate using custom rules
                if (!string.IsNullOrEmpty(config.ValidationRules))
                {
                    await ValidateUsingCustomRules(config.ValidationRules, value, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration {Key}", key);
                result.IsValid = false;
                result.Errors.Add("Validation error occurred");
                return result;
            }
        }

        public async Task NotifyServicesOfConfigurationChangeAsync(string key, object oldValue, object newValue)
        {
            try
            {
                // Notify specific services based on configuration key
                if (key.StartsWith("retention."))
                {
                    await NotifyDataRetentionService(key, newValue);
                }
                else if (key.StartsWith("generation."))
                {
                    await NotifyAutoGenerationService(key, newValue);
                }
                else if (key.StartsWith("services."))
                {
                    await NotifyBackgroundServices(key, newValue);
                }

                _logger.LogInformation("Configuration change notification sent for {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying services of configuration change: {Key}", key);
            }
        }

        public async Task<List<SystemConfiguration>> SearchConfigurationsAsync(ConfigurationSearchDto search)
        {
            try
            {
                var query = _context.SystemConfigurations.AsQueryable();

                if (!string.IsNullOrEmpty(search.SearchTerm))
                {
                    query = query.Where(c => c.Key.Contains(search.SearchTerm) ||
                                           c.DisplayName.Contains(search.SearchTerm) ||
                                           c.Description.Contains(search.SearchTerm));
                }

                if (!string.IsNullOrEmpty(search.Category))
                {
                    query = query.Where(c => c.Category == search.Category);
                }

                if (!string.IsNullOrEmpty(search.Section))
                {
                    query = query.Where(c => c.Section == search.Section);
                }

                if (search.IsAdvanced.HasValue)
                {
                    query = query.Where(c => c.IsAdvanced == search.IsAdvanced.Value);
                }

                if (search.RequiresRestart.HasValue)
                {
                    query = query.Where(c => c.RequiresRestart == search.RequiresRestart.Value);
                }

                if (search.IsReadOnly.HasValue)
                {
                    query = query.Where(c => c.IsReadOnly == search.IsReadOnly.Value);
                }

                // Apply sorting
                query = search.SortBy.ToLower() switch
                {
                    "key" => search.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(c => c.Key)
                        : query.OrderBy(c => c.Key),
                    "displayname" => search.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(c => c.DisplayName)
                        : query.OrderBy(c => c.DisplayName),
                    "category" => search.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(c => c.Category)
                        : query.OrderBy(c => c.Category),
                    "section" => search.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(c => c.Section)
                        : query.OrderBy(c => c.Section),
                    _ => search.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(c => c.DisplayOrder)
                        : query.OrderBy(c => c.DisplayOrder)
                };

                return await query
                    .Skip((search.Page - 1) * search.PageSize)
                    .Take(search.PageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching configurations");
                return new List<SystemConfiguration>();
            }
        }

        public async Task<ConfigurationImportResultDto> ImportConfigurationsAsync(ConfigurationImportDto import)
        {
            var result = new ConfigurationImportResultDto();

            try
            {
                Dictionary<string, object>? data = import.Format.ToLower() switch
                {
                    "json" => JsonSerializer.Deserialize<Dictionary<string, object>>(import.Data),
                    _ => throw new NotSupportedException($"Import format '{import.Format}' is not supported")
                };

                if (data == null)
                {
                    result.Errors.Add("Failed to parse import data");
                    return result;
                }

                result.TotalConfigurations = data.Count;

                foreach (var kvp in data)
                {
                    try
                    {
                        var exists = await ConfigurationExistsAsync(kvp.Key);

                        if (exists && !import.OverwriteExisting)
                        {
                            result.SkippedConfigurations++;
                            result.Warnings.Add($"Configuration '{kvp.Key}' already exists and overwrite is disabled");
                            continue;
                        }

                        if (!import.ValidateOnly)
                        {
                            var success = await SetConfigurationValueAsync(kvp.Key, kvp.Value, import.ImportedBy);
                            if (success)
                            {
                                result.ImportedConfigurations++;
                            }
                            else
                            {
                                result.FailedConfigurations++;
                                result.Errors.Add($"Failed to import configuration '{kvp.Key}'");
                            }
                        }
                        else
                        {
                            var validation = await ValidateConfigurationAsync(kvp.Key, kvp.Value.ToString() ?? string.Empty);
                            if (validation.IsValid)
                            {
                                result.ImportedConfigurations++;
                            }
                            else
                            {
                                result.FailedConfigurations++;
                                result.Errors.AddRange(validation.Errors.Select(e => $"{kvp.Key}: {e}"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedConfigurations++;
                        result.Errors.Add($"Error importing '{kvp.Key}': {ex.Message}");
                        _logger.LogError(ex, "Error importing configuration {Key}", kvp.Key);
                    }
                }

                result.Success = result.ImportedConfigurations > 0 || import.ValidateOnly;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during configuration import");
                result.Errors.Add($"Import failed: {ex.Message}");
                return result;
            }
        }

        public async Task<string> ExportConfigurationsAsync(ConfigurationExportDto export)
        {
            try
            {
                var query = _context.SystemConfigurations.AsQueryable();

                if (export.Categories?.Any() == true)
                {
                    query = query.Where(c => export.Categories.Contains(c.Category));
                }

                if (export.Sections?.Any() == true)
                {
                    query = query.Where(c => export.Sections.Contains(c.Section));
                }

                if (!export.IncludeAdvanced)
                {
                    query = query.Where(c => !c.IsAdvanced);
                }

                if (!export.IncludeReadOnly)
                {
                    query = query.Where(c => !c.IsReadOnly);
                }

                var configurations = await query.OrderBy(c => c.Category)
                    .ThenBy(c => c.Section)
                    .ThenBy(c => c.DisplayOrder)
                    .ToListAsync();

                return export.Format.ToLower() switch
                {
                    "json" => ExportToJson(configurations, export.IncludeMetadata),
                    _ => throw new NotSupportedException($"Export format '{export.Format}' is not supported")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting configurations");
                throw;
            }
        }

        public async Task ResetConfigurationToDefaultAsync(string key, string? updatedBy = null)
        {
            try
            {
                var config = await _context.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == key);

                if (config == null || string.IsNullOrEmpty(config.DefaultValue))
                {
                    _logger.LogWarning("Cannot reset configuration {Key}: not found or no default value", key);
                    return;
                }

                await SetConfigurationValueAsync(key, config.DefaultValue, updatedBy);
                _logger.LogInformation("Configuration {Key} reset to default value by {UpdatedBy}", key, updatedBy ?? "System");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting configuration {Key}", key);
            }
        }

        public async Task<bool> ConfigurationExistsAsync(string key)
        {
            try
            {
                return await _context.SystemConfigurations
                    .AnyAsync(c => c.Key == key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking configuration existence for key: {Key}", key);
                return false;
            }
        }

        // Private helper methods
        private async Task RefreshCacheIfNeeded()
        {
            if (DateTime.UtcNow - _lastCacheUpdate > _cacheExpiry)
            {
                await _cacheLock.WaitAsync();
                try
                {
                    if (DateTime.UtcNow - _lastCacheUpdate > _cacheExpiry)
                    {
                        _cache.Clear();
                        _lastCacheUpdate = DateTime.UtcNow;
                    }
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
        }

        private T? ParseValue<T>(string value, T? defaultValue = default)
        {
            try
            {
                var targetType = typeof(T);
                if (targetType == typeof(string))
                    return (T)(object)value;

                if (targetType == typeof(int))
                    return int.TryParse(value, out var intVal) ? (T)(object)intVal : defaultValue;

                if (targetType == typeof(bool))
                    return bool.TryParse(value, out var boolVal) ? (T)(object)boolVal : defaultValue;

                if (targetType == typeof(decimal))
                    return decimal.TryParse(value, out var decVal) ? (T)(object)decVal : defaultValue;

                if (targetType == typeof(DateTime))
                    return DateTime.TryParse(value, out var dateVal) ? (T)(object)dateVal : defaultValue;

                if (targetType == typeof(TimeSpan))
                    return TimeSpan.TryParse(value, out var timeVal) ? (T)(object)timeVal : defaultValue;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private object ParseValue(string value, string dataType)
        {
            return dataType.ToLower() switch
            {
                "int" => int.TryParse(value, out var intVal) ? intVal : 0,
                "bool" => bool.TryParse(value, out var boolVal) && boolVal,
                "decimal" => decimal.TryParse(value, out var decVal) ? decVal : 0m,
                "datetime" => DateTime.TryParse(value, out var dateVal) ? dateVal : DateTime.MinValue,
                "timespan" => TimeSpan.TryParse(value, out var timeVal) ? timeVal : TimeSpan.Zero,
                _ => value
            };
        }

        private object? TryParseValue(string value, string dataType)
        {
            try
            {
                return dataType.ToLower() switch
                {
                    "int" => int.Parse(value),
                    "bool" => bool.Parse(value),
                    "decimal" => decimal.Parse(value),
                    "datetime" => DateTime.Parse(value),
                    "timespan" => TimeSpan.Parse(value),
                    _ => value
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task ValidateUsingCustomRules(string rulesJson, string value, ConfigurationValidationResultDto result)
        {
            try
            {
                var rules = JsonSerializer.Deserialize<Dictionary<string, object>>(rulesJson);
                if (rules == null) return;

                // Implement custom validation rules
                if (rules.TryGetValue("min", out var minVal) && double.TryParse(value, out var numVal))
                {
                    var min = Convert.ToDouble(minVal);
                    if (numVal < min)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Value must be at least {min}");
                    }
                }

                if (rules.TryGetValue("max", out var maxVal) && double.TryParse(value, out numVal))
                {
                    var max = Convert.ToDouble(maxVal);
                    if (numVal > max)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Value must be at most {max}");
                    }
                }

                if (rules.TryGetValue("minLength", out var minLenVal))
                {
                    var minLen = Convert.ToInt32(minLenVal);
                    if (value.Length < minLen)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Value must be at least {minLen} characters long");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying custom validation rules");
                result.Warnings.Add("Some validation rules could not be applied");
            }

            await Task.CompletedTask;
        }

        private async Task NotifyDataRetentionService(string key, object newValue)
        {
            try
            {
                var retentionService = _serviceProvider.GetService<DataRetentionService>();
                // In a real implementation, the retention service would listen for configuration changes
                _logger.LogInformation("Notifying DataRetentionService of configuration change: {Key}", key);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying DataRetentionService");
            }
        }

        private async Task NotifyAutoGenerationService(string key, object newValue)
        {
            try
            {
                var generationService = _serviceProvider.GetService<AutoScenarioGenerationService>();
                // In a real implementation, the generation service would listen for configuration changes
                _logger.LogInformation("Notifying AutoScenarioGenerationService of configuration change: {Key}", key);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying AutoScenarioGenerationService");
            }
        }

        private async Task NotifyBackgroundServices(string key, object newValue)
        {
            try
            {
                _logger.LogInformation("Notifying background services of configuration change: {Key}", key);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying background services");
            }
        }

        private string ExportToJson(List<SystemConfiguration> configurations, bool includeMetadata)
        {
            if (includeMetadata)
            {
                var fullExport = configurations.Select(c => new
                {
                    c.Key,
                    c.Value,
                    c.Category,
                    c.DataType,
                    c.Description,
                    c.DisplayName,
                    c.Section,
                    c.DefaultValue,
                    c.IsReadOnly,
                    c.RequiresRestart,
                    c.IsAdvanced,
                    c.UpdatedAt,
                    c.UpdatedBy
                });
                return JsonSerializer.Serialize(fullExport, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                var simpleExport = configurations.ToDictionary(c => c.Key, c => c.Value);
                return JsonSerializer.Serialize(simpleExport, new JsonSerializerOptions { WriteIndented = true });
            }
        }
    }

    // Event args for configuration changes
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public string? UpdatedBy { get; }

        public ConfigurationChangedEventArgs(string key, object oldValue, object newValue, string? updatedBy)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
            UpdatedBy = updatedBy;
        }
    }
}
