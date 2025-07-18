using System.Text.Json.Serialization;

namespace WindowsSettingsMCP.Server.Models;

/// <summary>
/// Schema definitions for Windows system tools
/// </summary>
public static class ToolSchemas
{
    /// <summary>
    /// Schema for brightness adjustment tool
    /// </summary>
    public static object BrightnessSchema => new
    {
        type = "object",
        properties = new
        {
            action = new
            {
                type = "string",
                @enum = new[] { "get", "set" },
                description = "Whether to get current brightness or set new brightness"
            },
            value = new
            {
                type = "number",
                minimum = 0,
                maximum = 100,
                description = "Brightness level (0-100%). Required when action is 'set'"
            }
        },
        required = new[] { "action" }
    };

    /// <summary>
    /// Schema for volume adjustment tool
    /// </summary>
    public static object VolumeSchema => new
    {
        type = "object",
        properties = new
        {
            action = new
            {
                type = "string",
                @enum = new[] { "get", "set", "mute", "unmute" },
                description = "Volume action to perform"
            },
            value = new
            {
                type = "number",
                minimum = 0,
                maximum = 100,
                description = "Volume level (0-100%). Required when action is 'set'"
            }
        },
        required = new[] { "action" }
    };

    /// <summary>
    /// Schema for system information tool
    /// </summary>
    public static object SystemInfoSchema => new
    {
        type = "object",
        properties = new
        {
            category = new
            {
                type = "string",
                @enum = new[] { "all", "hardware", "display", "audio", "power" },
                description = "Category of system information to retrieve"
            }
        },
        required = new[] { "category" }
    };

    /// <summary>
    /// Schema for power plan management
    /// </summary>
    public static object PowerPlanSchema => new
    {
        type = "object",
        properties = new
        {
            action = new
            {
                type = "string",
                @enum = new[] { "get", "set", "list" },
                description = "Power plan action to perform"
            },
            planName = new
            {
                type = "string",
                description = "Name of the power plan. Required when action is 'set'"
            }
        },
        required = new[] { "action" }
    };
}

/// <summary>
/// Request/Response models for tool operations
/// </summary>
public class BrightnessRequest
{
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("value")]
    public int? Value { get; set; }
}

public class VolumeRequest
{
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("value")]
    public int? Value { get; set; }
}

public class SystemInfoRequest
{
    [JsonPropertyName("category")]
    public required string Category { get; set; }
}

public class PowerPlanRequest
{
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("planName")]
    public string? PlanName { get; set; }
}
