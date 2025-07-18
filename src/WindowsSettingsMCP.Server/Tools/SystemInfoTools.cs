using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindowsSettingsMCP.Server.Models;
using WindowsSettingsMCP.Server.Services;

namespace WindowsSettingsMCP.Server.Tools;

/// <summary>
/// Tool for retrieving Windows system information
/// </summary>
public class SystemInfoTools
{
    private readonly WindowsApiService _windowsApiService;
    private readonly AudioService _audioService;
    private readonly ILogger<SystemInfoTools> _logger;

    public SystemInfoTools(WindowsApiService windowsApiService, AudioService audioService, ILogger<SystemInfoTools> logger)
    {
        _windowsApiService = windowsApiService;
        _audioService = audioService;
        _logger = logger;
    }

    /// <summary>
    /// Get the system info tool definition
    /// </summary>
    public McpTool GetToolDefinition()
    {
        return new McpTool
        {
            Name = "get_system_info",
            Description = "Retrieve Windows system information including hardware, display, audio, and power settings. Categories: 'all', 'hardware', 'display', 'audio', 'power'.",
            InputSchema = ToolSchemas.SystemInfoSchema
        };
    }

    /// <summary>
    /// Execute the system info tool
    /// </summary>
    /// <param name="arguments">Tool arguments</param>
    /// <returns>Tool execution result</returns>
    public async Task<ToolCallResult> ExecuteAsync(Dictionary<string, object>? arguments)
    {
        try
        {
            if (arguments == null)
            {
                return CreateErrorResult("No arguments provided");
            }

            // Parse arguments
            var jsonArgs = JsonSerializer.Serialize(arguments);
            var request = JsonSerializer.Deserialize<SystemInfoRequest>(jsonArgs);

            if (request == null || string.IsNullOrEmpty(request.Category))
            {
                return CreateErrorResult("Invalid arguments: category is required");
            }

            _logger.LogInformation("Retrieving system info for category: {Category}", request.Category);

            var systemInfo = new Dictionary<string, object>();

            switch (request.Category.ToLower())
            {
                case "all":
                    await AddAllSystemInfoAsync(systemInfo);
                    break;

                case "hardware":
                    var hardwareInfo = await _windowsApiService.GetSystemInfoAsync("hardware");
                    foreach (var kvp in hardwareInfo)
                    {
                        systemInfo[kvp.Key] = kvp.Value;
                    }
                    break;

                case "display":
                    var displayInfo = await _windowsApiService.GetSystemInfoAsync("display");
                    foreach (var kvp in displayInfo)
                    {
                        systemInfo[kvp.Key] = kvp.Value;
                    }
                    break;

                case "audio":
                    var audioInfo = await _audioService.GetAudioDeviceInfoAsync();
                    foreach (var kvp in audioInfo)
                    {
                        systemInfo[kvp.Key] = kvp.Value;
                    }
                    break;

                case "power":
                    var powerInfo = await _windowsApiService.GetSystemInfoAsync("power");
                    foreach (var kvp in powerInfo)
                    {
                        systemInfo[kvp.Key] = kvp.Value;
                    }
                    break;

                default:
                    return CreateErrorResult($"Unknown category: {request.Category}. Supported categories: all, hardware, display, audio, power");
            }

            // Format the result
            var formattedInfo = FormatSystemInfo(systemInfo, request.Category);

            return new ToolCallResult
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = formattedInfo
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing system info tool");
            return CreateErrorResult($"Error retrieving system information: {ex.Message}");
        }
    }

    private async Task AddAllSystemInfoAsync(Dictionary<string, object> systemInfo)
    {
        // Get hardware info
        var hardwareInfo = await _windowsApiService.GetSystemInfoAsync("hardware");
        foreach (var kvp in hardwareInfo)
        {
            systemInfo[$"hardware_{kvp.Key}"] = kvp.Value;
        }

        // Get display info
        var displayInfo = await _windowsApiService.GetSystemInfoAsync("display");
        foreach (var kvp in displayInfo)
        {
            systemInfo[$"display_{kvp.Key}"] = kvp.Value;
        }

        // Get audio info
        var audioInfo = await _audioService.GetAudioDeviceInfoAsync();
        foreach (var kvp in audioInfo)
        {
            systemInfo[$"audio_{kvp.Key}"] = kvp.Value;
        }

        // Get power info
        var powerInfo = await _windowsApiService.GetSystemInfoAsync("power");
        foreach (var kvp in powerInfo)
        {
            systemInfo[$"power_{kvp.Key}"] = kvp.Value;
        }
    }

    private static string FormatSystemInfo(Dictionary<string, object> info, string category)
    {
        var result = new List<string>();
        result.Add($"=== Windows System Information ({category.ToUpper()}) ===");
        result.Add("");

        foreach (var kvp in info.OrderBy(x => x.Key))
        {
            var key = kvp.Key.Replace("_", " ").ToTitleCase();
            var value = kvp.Value;

            if (value is List<object> list)
            {
                result.Add($"{key}:");
                foreach (var item in list)
                {
                    result.Add($"  - {FormatComplexValue(item)}");
                }
            }
            else
            {
                result.Add($"{key}: {FormatComplexValue(value)}");
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    private static string FormatComplexValue(object value)
    {
        if (value == null) return "N/A";
        
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ToString();
        }

        return value.ToString() ?? "N/A";
    }

    private static ToolCallResult CreateErrorResult(string message)
    {
        return new ToolCallResult
        {
            IsError = true,
            Content = new List<ContentItem>
            {
                new ContentItem
                {
                    Type = "text",
                    Text = message
                }
            }
        };
    }
}

/// <summary>
/// Extension method for converting strings to title case
/// </summary>
public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join(" ", words);
    }
}
