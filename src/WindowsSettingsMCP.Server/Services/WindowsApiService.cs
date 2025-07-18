using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace WindowsSettingsMCP.Server.Services;

/// <summary>
/// Service for interacting with Windows APIs and system functions
/// </summary>
public class WindowsApiService
{
    private readonly ILogger<WindowsApiService> _logger;

    // Windows API declarations
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

    [DllImport("gdi32.dll")]
    private static extern bool GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RAMP
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Red;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Green;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Blue;
    }

    public WindowsApiService(ILogger<WindowsApiService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get current display brightness using WMI
    /// </summary>
    /// <returns>Brightness level (0-100) or null if failed</returns>
    public async Task<int?> GetBrightnessAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\WMI", 
                "SELECT * FROM WmiMonitorBrightness");
            
            var collection = await Task.Run(() => searcher.Get());
            
            foreach (ManagementObject obj in collection)
            {
                var brightness = obj["CurrentBrightness"];
                if (brightness != null)
                {
                    _logger.LogInformation("Current brightness: {Brightness}%", brightness);
                    return Convert.ToInt32(brightness);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get brightness");
        }
        
        return null;
    }

    /// <summary>
    /// Set display brightness using WMI
    /// </summary>
    /// <param name="brightness">Brightness level (0-100)</param>
    /// <returns>True if successful</returns>
    public async Task<bool> SetBrightnessAsync(int brightness)
    {
        try
        {
            if (brightness < 0 || brightness > 100)
            {
                _logger.LogWarning("Invalid brightness value: {Brightness}. Must be 0-100", brightness);
                return false;
            }

            using var searcher = new ManagementObjectSearcher("root\\WMI", 
                "SELECT * FROM WmiMonitorBrightnessMethods");
            
            var collection = await Task.Run(() => searcher.Get());
            
            foreach (ManagementObject obj in collection)
            {
                await Task.Run(() => obj.InvokeMethod("WmiSetBrightness", new object[] { 1, brightness }));
                _logger.LogInformation("Set brightness to {Brightness}%", brightness);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set brightness to {Brightness}%", brightness);
        }
        
        return false;
    }

    /// <summary>
    /// Get system information
    /// </summary>
    /// <param name="category">Category of information to retrieve</param>
    /// <returns>Dictionary containing system information</returns>
    public async Task<Dictionary<string, object>> GetSystemInfoAsync(string category)
    {
        var info = new Dictionary<string, object>();
        
        try
        {
            switch (category.ToLower())
            {
                case "all":
                case "hardware":
                    await AddHardwareInfoAsync(info);
                    if (category.ToLower() != "all") break;
                    goto case "display";
                
                case "display":
                    await AddDisplayInfoAsync(info);
                    if (category.ToLower() != "all") break;
                    goto case "power";
                
                case "power":
                    await AddPowerInfoAsync(info);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system info for category: {Category}", category);
            info["error"] = ex.Message;
        }
        
        return info;
    }

    private async Task AddHardwareInfoAsync(Dictionary<string, object> info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            var collection = await Task.Run(() => searcher.Get());
            
            foreach (ManagementObject obj in collection)
            {
                info["computerName"] = obj["Name"]?.ToString() ?? "Unknown";
                info["manufacturer"] = obj["Manufacturer"]?.ToString() ?? "Unknown";
                info["model"] = obj["Model"]?.ToString() ?? "Unknown";
                info["totalPhysicalMemory"] = obj["TotalPhysicalMemory"]?.ToString() ?? "Unknown";
                break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hardware info");
        }
    }

    private async Task AddDisplayInfoAsync(Dictionary<string, object> info)
    {
        try
        {
            var brightness = await GetBrightnessAsync();
            if (brightness.HasValue)
            {
                info["brightness"] = brightness.Value;
            }

            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var collection = await Task.Run(() => searcher.Get());
            var displays = new List<object>();
            
            foreach (ManagementObject obj in collection)
            {
                displays.Add(new
                {
                    name = obj["Name"]?.ToString(),
                    adapterRAM = obj["AdapterRAM"]?.ToString(),
                    currentHorizontalResolution = obj["CurrentHorizontalResolution"]?.ToString(),
                    currentVerticalResolution = obj["CurrentVerticalResolution"]?.ToString()
                });
            }
            
            info["displays"] = displays;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get display info");
        }
    }

    private async Task AddPowerInfoAsync(Dictionary<string, object> info)
    {
        try
        {
            var scope = new ManagementScope("root\\cimv2\\power");
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM Win32_PowerPlan"));
            var collection = await Task.Run(() => searcher.Get());
            var powerPlans = new List<object>();
            
            foreach (ManagementObject obj in collection)
            {
                powerPlans.Add(new
                {
                    elementName = obj["ElementName"]?.ToString(),
                    isActive = Convert.ToBoolean(obj["IsActive"] ?? false)
                });
            }
            
            info["powerPlans"] = powerPlans;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get power info");
        }
    }
}
