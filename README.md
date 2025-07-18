# Windows Settings MCP Server

A Model Context Protocol (MCP) server for managing Windows system settings including brightness, volume, and system information retrieval.

## Features

### Available Tools

#### 1. Brightness Control (`adjust_brightness`)
- **Get current brightness**: Retrieve the current display brightness level
- **Set brightness**: Adjust display brightness (0-100%)
- Uses Windows WMI for hardware brightness control

#### 2. Volume Control (`adjust_volume`)
- **Get current volume**: Retrieve current system volume and mute status
- **Set volume**: Adjust system volume (0-100%)
- **Mute/Unmute**: Control audio mute state
- Uses NAudio library for Core Audio API integration

#### 3. System Information (`get_system_info`)
- **Hardware info**: Computer name, manufacturer, model, memory
- **Display info**: Current brightness, video controllers, resolutions
- **Audio info**: Default audio device, available devices, current settings
- **Power info**: Available power plans and current active plan
- **All**: Comprehensive system overview

## Requirements

- **Operating System**: Windows 10/11
- **.NET Runtime**: .NET 8.0 or later
- **Permissions**: Some features may require administrator privileges

## Installation

### Building from Source

1. Clone the repository:
```powershell
git clone <repository-url>
cd WindowsSettingsMCP
```

2. Build the project:
```powershell
dotnet build src/WindowsSettingsMCP.Server/WindowsSettingsMCP.Server.csproj -c Release
```

3. Run the server:
```powershell
dotnet run --project src/WindowsSettingsMCP.Server/WindowsSettingsMCP.Server.csproj
```

## Usage

### MCP Client Integration

This server implements the Model Context Protocol and communicates via JSON-RPC over stdin/stdout. It can be integrated with any MCP-compatible client.

### Tool Examples

#### Brightness Control
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "adjust_brightness",
    "arguments": {
      "action": "get"
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "adjust_brightness",
    "arguments": {
      "action": "set",
      "value": 75
    }
  }
}
```

#### Volume Control
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "adjust_volume",
    "arguments": {
      "action": "set",
      "value": 50
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "adjust_volume",
    "arguments": {
      "action": "mute"
    }
  }
}
```

#### System Information
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "get_system_info",
    "arguments": {
      "category": "display"
    }
  }
}
```

## MCP Client Configuration

### GitHub Copilot Chat

To use this server with GitHub Copilot Chat, use the `mcp.json` configuration:

```json
{
  "mcpServers": {
    "windows-settings": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "src/WindowsSettingsMCP.Server/WindowsSettingsMCP.Server.csproj"
      ],
      "cwd": "c:\\Users\\Evans\\Documents\\Projects\\WindowsSettingsMCP",
      "env": {
        "DOTNET_ENVIRONMENT": "Production"
      }
    }
  }
}
```

### Claude Desktop

For Claude Desktop, copy the content of `claude_desktop_config.json` to your Claude Desktop configuration file (typically located at `%APPDATA%\Claude\claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "windows-settings": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "c:\\Users\\Evans\\Documents\\Projects\\WindowsSettingsMCP\\src\\WindowsSettingsMCP.Server\\WindowsSettingsMCP.Server.csproj"
      ]
    }
  }
}
```

### Using the Tools

Once configured, you can interact with the server using natural language commands:

- **"Set my screen brightness to 80%"**
- **"What's my current volume level?"**
- **"Mute the system audio"**
- **"Show me my display information"**
- **"Get hardware details"**

The MCP client will automatically translate these requests into the appropriate tool calls.

## Architecture

### Project Structure
```
src/WindowsSettingsMCP.Server/
├── Models/           # MCP protocol and data models
├── Services/         # Windows API integration services
├── Tools/           # MCP tool implementations
├── McpServer.cs     # Main MCP server logic
└── Program.cs       # Application entry point
```

### Key Components

#### Services
- **WindowsApiService**: WMI integration for brightness and system info
- **AudioService**: NAudio integration for volume control

#### Tools
- **BrightnessTools**: Display brightness management
- **VolumeTools**: System volume and mute control
- **SystemInfoTools**: Windows system information retrieval

#### Models
- **McpMessage**: JSON-RPC message structures
- **ToolSchemas**: Input validation schemas for tools

## Dependencies

- **System.Management**: Windows WMI access
- **NAudio**: Windows Core Audio API integration
- **Microsoft.Extensions.Hosting**: Dependency injection and hosting
- **Microsoft.Extensions.Logging**: Structured logging
- **System.Text.Json**: JSON serialization

## Permissions

Some operations may require elevated permissions:

- **Brightness Control**: Usually works with standard user privileges
- **Volume Control**: Works with standard user privileges
- **System Information**: Most info available to standard users
- **Power Management**: May require administrator privileges

## Troubleshooting

### Common Issues

1. **Brightness control not working**
   - Ensure your display supports software brightness control
   - Some external monitors may not support WMI brightness control
   - Try running with administrator privileges

2. **Audio control not working**
   - Verify that audio devices are properly installed
   - Check Windows audio service is running
   - Ensure NAudio can access the default audio device

3. **Permission errors**
   - Some WMI queries may require administrator privileges
   - Run the server as administrator if needed

### Logging

The server logs to stderr (not stdout to avoid interfering with MCP protocol). Logs include:
- Tool execution details
- Error messages and stack traces
- Performance and diagnostic information

## License

[Add your license information here]

## Contributing

[Add contribution guidelines here]
