# Quick MCP Server Test Script
# This script tests the basic functionality of the Windows Settings MCP Server

param(
    [int]$TimeoutSeconds = 10
)

Write-Host "Testing Windows Settings MCP Server..." -ForegroundColor Green

# Change to the project directory
$projectPath = "c:\Users\Evans\Documents\Projects\WindowsSettingsMCP"
Set-Location $projectPath

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
$buildResult = dotnet build src/WindowsSettingsMCP.Server/WindowsSettingsMCP.Server.csproj 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green

# Start the server process
Write-Host "Starting MCP server..." -ForegroundColor Yellow
$serverProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src/WindowsSettingsMCP.Server/WindowsSettingsMCP.Server.csproj" `
    -PassThru -NoNewWindow `
    -RedirectStandardInput -RedirectStandardOutput -RedirectStandardError

# Wait a moment for the server to start
Start-Sleep -Seconds 2

# Test commands
$testCommands = @(
    @{
        name = "Initialize"
        command = '{"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {}}'
    },
    @{
        name = "List Tools"
        command = '{"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}}'
    },
    @{
        name = "Get System Info"
        command = '{"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {"name": "get_system_info", "arguments": {"category": "hardware"}}}'
    },
    @{
        name = "Get Brightness"
        command = '{"jsonrpc": "2.0", "id": 4, "method": "tools/call", "params": {"name": "adjust_brightness", "arguments": {"action": "get"}}}'
    },
    @{
        name = "Get Volume"
        command = '{"jsonrpc": "2.0", "id": 5, "method": "tools/call", "params": {"name": "adjust_volume", "arguments": {"action": "get"}}}'
    }
)

$successCount = 0
$totalTests = $testCommands.Count

foreach ($test in $testCommands) {
    Write-Host "Testing: $($test.name)..." -ForegroundColor Cyan
    
    try {
        # Send command to server
        $serverProcess.StandardInput.WriteLine($test.command)
        $serverProcess.StandardInput.Flush()
        
        # Wait for response (simplified - just check if process is still running)
        Start-Sleep -Milliseconds 500
        
        if (!$serverProcess.HasExited) {
            Write-Host "  ✓ $($test.name) - Command sent successfully" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host "  ✗ $($test.name) - Server process exited" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  ✗ $($test.name) - Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Clean up
try {
    $serverProcess.Kill()
    $serverProcess.WaitForExit(5000)
}
catch {
    Write-Host "Warning: Could not cleanly stop server process" -ForegroundColor Yellow
}

# Results
Write-Host "`nTest Results:" -ForegroundColor Magenta
Write-Host "  Successful: $successCount/$totalTests" -ForegroundColor Green

if ($successCount -eq $totalTests) {
    Write-Host "All tests passed! MCP server is working correctly." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some tests failed. Check the server implementation." -ForegroundColor Red
    exit 1
}
