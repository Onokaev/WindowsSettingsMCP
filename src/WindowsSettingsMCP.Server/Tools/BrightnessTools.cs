using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindowsSettingsMCP.Server.Models;
using WindowsSettingsMCP.Server.Services;

namespace WindowsSettingsMCP.Server.Tools;

/// <summary>
/// Tool for managing display brightness
/// </summary>
public class BrightnessTools
{
    private readonly WindowsApiService _windowsApiService;
    private readonly ILogger<BrightnessTools> _logger;

    public BrightnessTools(WindowsApiService windowsApiService, ILogger<BrightnessTools> logger)
    {
        _windowsApiService = windowsApiService;
        _logger = logger;
    }

    /// <summary>
    /// Get the brightness tool definition
    /// </summary>
    public McpTool GetToolDefinition()
    {
        return new McpTool
        {
            Name = "adjust_brightness",
            Description = "Get or set the display brightness level. Use 'get' to retrieve current brightness or 'set' to change brightness (0-100%).",
            InputSchema = ToolSchemas.BrightnessSchema
        };
    }

    /// <summary>
    /// Execute the brightness tool
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
            var request = JsonSerializer.Deserialize<BrightnessRequest>(jsonArgs);

            if (request == null || string.IsNullOrEmpty(request.Action))
            {
                return CreateErrorResult("Invalid arguments: action is required");
            }

            _logger.LogInformation("Executing brightness action: {Action}", request.Action);

            switch (request.Action.ToLower())
            {
                case "get":
                    return await GetBrightnessAsync();

                case "set":
                    if (!request.Value.HasValue)
                    {
                        return CreateErrorResult("Value is required when action is 'set'");
                    }
                    return await SetBrightnessAsync(request.Value.Value);

                default:
                    return CreateErrorResult($"Unknown action: {request.Action}. Supported actions: get, set");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing brightness tool");
            return CreateErrorResult($"Error executing brightness tool: {ex.Message}");
        }
    }

    private async Task<ToolCallResult> GetBrightnessAsync()
    {
        var brightness = await _windowsApiService.GetBrightnessAsync();
        
        if (brightness.HasValue)
        {
            return new ToolCallResult
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = $"Current display brightness: {brightness.Value}%"
                    }
                }
            };
        }
        else
        {
            return CreateErrorResult("Failed to retrieve brightness level. This may be due to unsupported hardware or insufficient permissions.");
        }
    }

    private async Task<ToolCallResult> SetBrightnessAsync(int value)
    {
        if (value < 0 || value > 100)
        {
            return CreateErrorResult("Brightness value must be between 0 and 100");
        }

        var success = await _windowsApiService.SetBrightnessAsync(value);
        
        if (success)
        {
            return new ToolCallResult
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = $"Successfully set display brightness to {value}%"
                    }
                }
            };
        }
        else
        {
            return CreateErrorResult($"Failed to set brightness to {value}%. This may be due to unsupported hardware or insufficient permissions.");
        }
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
