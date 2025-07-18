using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindowsSettingsMCP.Server;
using WindowsSettingsMCP.Server.Services;
using WindowsSettingsMCP.Server.Tools;

namespace WindowsSettingsMCP.Server;

/// <summary>
/// Main program entry point for Windows Settings MCP Server
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Create host builder with dependency injection
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                    
                    // Log to stderr so it doesn't interfere with MCP protocol on stdout
                    builder.AddConsole(options =>
                    {
                        options.LogToStandardErrorThreshold = LogLevel.Trace;
                    });
                });

                // Register services
                services.AddSingleton<WindowsApiService>();
                services.AddSingleton<AudioService>();
                
                // Register tools
                services.AddSingleton<BrightnessTools>();
                services.AddSingleton<VolumeTools>();
                services.AddSingleton<SystemInfoTools>();
                
                // Register main server
                services.AddSingleton<McpServer>();
            })
            .Build();

        // Get the MCP server from DI container
        var mcpServer = host.Services.GetRequiredService<McpServer>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting Windows Settings MCP Server...");
            
            // Create cancellation token for graceful shutdown
            using var cts = new CancellationTokenSource();
            
            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                logger.LogInformation("Shutdown requested...");
                cts.Cancel();
            };

            // Run the server
            await mcpServer.RunAsync(cts.Token);
            
            logger.LogInformation("Windows Settings MCP Server stopped.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error occurred");
            Environment.Exit(1);
        }
    }
}
