using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindowsSettingsMCP.Server.Models;
using WindowsSettingsMCP.Server.Services;

namespace WindowsSettingsMCP.Server.Tools;

/// <summary>
/// Tool for managing system volume
/// </summary>
public class VolumeTools
{
    private readonly AudioService _audioService;
    private readonly ILogger<VolumeTools> _logger;

    public VolumeTools(AudioService audioService, ILogger<VolumeTools> logger)
    {
        _audioService = audioService;
        _logger = logger;
    }

    /// <summary>
    /// Get the volume tool definition
    /// </summary>
    public McpTool GetToolDefinition()
    {
        return new McpTool
        {
            Name = "adjust_volume",
            Description = "Get or set the system volume level, or mute/unmute audio. Use 'get' to retrieve current volume, 'set' to change volume (0-100%), 'mute' to mute audio, or 'unmute' to unmute audio.",
            InputSchema = ToolSchemas.VolumeSchema
        };
    }

    /// <summary>
    /// Execute the volume tool
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
            var request = JsonSerializer.Deserialize<VolumeRequest>(jsonArgs);

            if (request == null || string.IsNullOrEmpty(request.Action))
            {
                return CreateErrorResult("Invalid arguments: action is required");
            }

            _logger.LogInformation("Executing volume action: {Action}", request.Action);

            switch (request.Action.ToLower())
            {
                case "get":
                    return await GetVolumeAsync();

                case "set":
                    if (!request.Value.HasValue)
                    {
                        return CreateErrorResult("Value is required when action is 'set'");
                    }
                    return await SetVolumeAsync(request.Value.Value);

                case "mute":
                    return await SetMuteAsync(true);

                case "unmute":
                    return await SetMuteAsync(false);

                default:
                    return CreateErrorResult($"Unknown action: {request.Action}. Supported actions: get, set, mute, unmute");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing volume tool");
            return CreateErrorResult($"Error executing volume tool: {ex.Message}");
        }
    }

    private async Task<ToolCallResult> GetVolumeAsync()
    {
        var volume = await _audioService.GetVolumeAsync();
        var isMuted = await _audioService.GetMuteStatusAsync();
        
        if (volume.HasValue && isMuted.HasValue)
        {
            var status = isMuted.Value ? " (muted)" : "";
            return new ToolCallResult
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = $"Current system volume: {volume.Value}%{status}"
                    }
                }
            };
        }
        else
        {
            return CreateErrorResult("Failed to retrieve volume level");
        }
    }

    private async Task<ToolCallResult> SetVolumeAsync(int value)
    {
        if (value < 0 || value > 100)
        {
            return CreateErrorResult("Volume value must be between 0 and 100");
        }

        var success = await _audioService.SetVolumeAsync(value);
        
        if (success)
        {
            return new ToolCallResult
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = $"Successfully set system volume to {value}%"
                    }
                }
            };
        }
        else
        {
            return CreateErrorResult($"Failed to set volume to {value}%");
        }
    }

    private async Task<ToolCallResult> SetMuteAsync(bool mute)
    {
        var success = await _audioService.SetMuteAsync(mute);
        var action = mute ? "muted" : "unmuted";
        
        if (success)
        {
            return new ToolCallResult
            {
                Content = new List<ContentItem>
                {
                    new ContentItem
                    {
                        Type = "text",
                        Text = $"Successfully {action} system audio"
                    }
                }
            };
        }
        else
        {
            return CreateErrorResult($"Failed to {(mute ? "mute" : "unmute")} system audio");
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
