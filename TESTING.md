# Windows Settings MCP Server - Quick Test

## Test the Basic MCP Protocol

You can test the server by running it and sending JSON-RPC messages via stdin:

### 1. Start the server
```powershell
cd "c:\Users\Evans\Documents\Projects\WindowsSettingsMCP"
dotnet run --project src/WindowsSettingsMCP.Server/WindowsSettingsMCP.Server.csproj
```

### 2. Initialize the connection
Send this JSON to stdin:
```json
{"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {}}
```

### 3. List available tools
```json
{"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}}
```

### 4. Test brightness control
Get current brightness:
```json
{"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {"name": "adjust_brightness", "arguments": {"action": "get"}}}
```

Set brightness to 50%:
```json
{"jsonrpc": "2.0", "id": 4, "method": "tools/call", "params": {"name": "adjust_brightness", "arguments": {"action": "set", "value": 50}}}
```

### 5. Test volume control
Get current volume:
```json
{"jsonrpc": "2.0", "id": 5, "method": "tools/call", "params": {"name": "adjust_volume", "arguments": {"action": "get"}}}
```

Set volume to 30%:
```json
{"jsonrpc": "2.0", "id": 6, "method": "tools/call", "params": {"name": "adjust_volume", "arguments": {"action": "set", "value": 30}}}
```

### 6. Test system information
Get display information:
```json
{"jsonrpc": "2.0", "id": 7, "method": "tools/call", "params": {"name": "get_system_info", "arguments": {"category": "display"}}}
```

## Expected Responses

Each request should return a JSON-RPC response with either a `result` or `error` field.

## Automation Script

You can also create a PowerShell script to test the server automatically:

```powershell
# test-mcp-server.ps1
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/WindowsSettingsMCP.Server/WindowsSettingsMCP.Server.csproj" -PassThru -RedirectStandardInput -RedirectStandardOutput

# Send test commands
$commands = @(
    '{"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {}}',
    '{"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}}',
    '{"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {"name": "get_system_info", "arguments": {"category": "hardware"}}}'
)

foreach ($command in $commands) {
    $serverProcess.StandardInput.WriteLine($command)
    Start-Sleep -Milliseconds 500
}

$serverProcess.Kill()
```
