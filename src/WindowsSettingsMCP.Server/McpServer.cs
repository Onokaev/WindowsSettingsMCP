using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindowsSettingsMCP.Server.Models;
using WindowsSettingsMCP.Server.Tools;
using WindowsSettingsMCP.Server.Services;

namespace WindowsSettingsMCP.Server;

/// <summary>
/// Main MCP Server implementation for Windows settings management
/// </summary>
public class McpServer
{
    private readonly ILogger<McpServer> _logger;
    private readonly BrightnessTools _brightnessTools;
    private readonly VolumeTools _volumeTools;
    private readonly SystemInfoTools _systemInfoTools;
    private readonly Dictionary<string, McpTool> _tools;

    public McpServer(
        ILogger<McpServer> logger,
        BrightnessTools brightnessTools,
        VolumeTools volumeTools,
        SystemInfoTools systemInfoTools)
    {
        _logger = logger;
        _brightnessTools = brightnessTools;
        _volumeTools = volumeTools;
        _systemInfoTools = systemInfoTools;

        // Register all tools
        _tools = new Dictionary<string, McpTool>
        {
            { "adjust_brightness", _brightnessTools.GetToolDefinition() },
            { "adjust_volume", _volumeTools.GetToolDefinition() },
            { "get_system_info", _systemInfoTools.GetToolDefinition() }
        };

        _logger.LogInformation("MCP Server initialized with {ToolCount} tools", _tools.Count);
    }

    /// <summary>
    /// Start the MCP server and handle stdin/stdout communication
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Windows Settings MCP Server starting...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var input = await Console.In.ReadLineAsync();
                if (input == null)
                {
                    _logger.LogInformation("Received EOF, shutting down server");
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                try
                {
                    var request = JsonSerializer.Deserialize<McpRequest>(input);
                    if (request != null)
                    {
                        var response = await HandleRequestAsync(request);
                        var responseJson = JsonSerializer.Serialize(response);
                        await Console.Out.WriteLineAsync(responseJson);
                        await Console.Out.FlushAsync();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON input: {Input}", input);
                    var errorResponse = CreateErrorResponse(null, -32700, "Parse error");
                    var errorJson = JsonSerializer.Serialize(errorResponse);
                    await Console.Out.WriteLineAsync(errorJson);
                    await Console.Out.FlushAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in MCP server");
            throw;
        }
    }

    /// <summary>
    /// Handle incoming MCP requests
    /// </summary>
    private async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        _logger.LogDebug("Handling request: {Method}", request.Method);

        try
        {
            switch (request.Method)
            {
                case "initialize":
                    return HandleInitialize(request);

                case "tools/list":
                    return HandleToolsList(request);

                case "tools/call":
                    return await HandleToolCallAsync(request);

                case "ping":
                    return HandlePing(request);

                default:
                    _logger.LogWarning("Unknown method: {Method}", request.Method);
                    return CreateErrorResponse(request.Id, -32601, $"Method not found: {request.Method}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling request: {Method}", request.Method);
            return CreateErrorResponse(request.Id, -32603, $"Internal error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle MCP initialize request
    /// </summary>
    private McpResponse HandleInitialize(McpRequest request)
    {
        _logger.LogInformation("Client initializing MCP connection");
        
        var result = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { },
                logging = new { }
            },
            serverInfo = new
            {
                name = "Windows Settings MCP Server",
                version = "1.0.0"
            }
        };

        return new McpResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    /// <summary>
    /// Handle tools list request
    /// </summary>
    private McpResponse HandleToolsList(McpRequest request)
    {
        _logger.LogDebug("Returning list of {ToolCount} tools", _tools.Count);
        
        var result = new
        {
            tools = _tools.Values.ToArray()
        };

        return new McpResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    /// <summary>
    /// Handle tool call request
    /// </summary>
    private async Task<McpResponse> HandleToolCallAsync(McpRequest request)
    {
        if (request.Params == null)
        {
            return CreateErrorResponse(request.Id, -32602, "Invalid params: params are required for tool calls");
        }

        try
        {
            var paramsJson = JsonSerializer.Serialize(request.Params);
            var toolCallParams = JsonSerializer.Deserialize<ToolCallParams>(paramsJson);

            if (toolCallParams == null || string.IsNullOrEmpty(toolCallParams.Name))
            {
                return CreateErrorResponse(request.Id, -32602, "Invalid params: tool name is required");
            }

            _logger.LogInformation("Executing tool: {ToolName}", toolCallParams.Name);

            ToolCallResult? result = toolCallParams.Name switch
            {
                "adjust_brightness" => await _brightnessTools.ExecuteAsync(toolCallParams.Arguments),
                "adjust_volume" => await _volumeTools.ExecuteAsync(toolCallParams.Arguments),
                "get_system_info" => await _systemInfoTools.ExecuteAsync(toolCallParams.Arguments),
                _ => null
            };

            if (result == null)
            {
                return CreateErrorResponse(request.Id, -32601, $"Unknown tool: {toolCallParams.Name}");
            }

            return new McpResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool call");
            return CreateErrorResponse(request.Id, -32603, $"Tool execution error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle ping request
    /// </summary>
    private McpResponse HandlePing(McpRequest request)
    {
        return new McpResponse
        {
            Id = request.Id,
            Result = new { }
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    private static McpResponse CreateErrorResponse(object? id, int code, string message)
    {
        return new McpResponse
        {
            Id = id,
            Error = new McpError
            {
                Code = code,
                Message = message
            }
        };
    }
}
