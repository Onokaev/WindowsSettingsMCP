using NAudio.CoreAudioApi;
using Microsoft.Extensions.Logging;

namespace WindowsSettingsMCP.Server.Services;

/// <summary>
/// Service for managing Windows audio settings using NAudio
/// </summary>
public class AudioService
{
    private readonly ILogger<AudioService> _logger;
    private readonly MMDeviceEnumerator _deviceEnumerator;

    public AudioService(ILogger<AudioService> logger)
    {
        _logger = logger;
        _deviceEnumerator = new MMDeviceEnumerator();
    }

    /// <summary>
    /// Get current system volume
    /// </summary>
    /// <returns>Volume level (0-100) or null if failed</returns>
    public async Task<int?> GetVolumeAsync()
    {
        try
        {
            var device = await Task.Run(() => _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));
            var volume = device.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
            
            _logger.LogInformation("Current volume: {Volume}%", (int)volume);
            return (int)volume;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get volume");
            return null;
        }
    }

    /// <summary>
    /// Set system volume
    /// </summary>
    /// <param name="volume">Volume level (0-100)</param>
    /// <returns>True if successful</returns>
    public async Task<bool> SetVolumeAsync(int volume)
    {
        try
        {
            if (volume < 0 || volume > 100)
            {
                _logger.LogWarning("Invalid volume value: {Volume}. Must be 0-100", volume);
                return false;
            }

            var device = await Task.Run(() => _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));
            var volumeLevel = volume / 100.0f;
            
            await Task.Run(() => device.AudioEndpointVolume.MasterVolumeLevelScalar = volumeLevel);
            
            _logger.LogInformation("Set volume to {Volume}%", volume);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set volume to {Volume}%", volume);
            return false;
        }
    }

    /// <summary>
    /// Get current mute status
    /// </summary>
    /// <returns>True if muted, false if not muted, null if failed</returns>
    public async Task<bool?> GetMuteStatusAsync()
    {
        try
        {
            var device = await Task.Run(() => _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));
            var isMuted = device.AudioEndpointVolume.Mute;
            
            _logger.LogInformation("Current mute status: {IsMuted}", isMuted);
            return isMuted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mute status");
            return null;
        }
    }

    /// <summary>
    /// Mute or unmute system audio
    /// </summary>
    /// <param name="mute">True to mute, false to unmute</param>
    /// <returns>True if successful</returns>
    public async Task<bool> SetMuteAsync(bool mute)
    {
        try
        {
            var device = await Task.Run(() => _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));
            await Task.Run(() => device.AudioEndpointVolume.Mute = mute);
            
            _logger.LogInformation("Audio {Action}", mute ? "muted" : "unmuted");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to {Action} audio", mute ? "mute" : "unmute");
            return false;
        }
    }

    /// <summary>
    /// Get audio device information
    /// </summary>
    /// <returns>Dictionary containing audio device information</returns>
    public async Task<Dictionary<string, object>> GetAudioDeviceInfoAsync()
    {
        var info = new Dictionary<string, object>();
        
        try
        {
            var device = await Task.Run(() => _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));
            
            info["deviceName"] = device.FriendlyName;
            info["deviceId"] = device.ID;
            info["state"] = device.State.ToString();
            info["volume"] = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            info["isMuted"] = device.AudioEndpointVolume.Mute;
            
            // Get all audio devices
            var devices = await Task.Run(() => _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active));
            var deviceList = new List<object>();
            
            foreach (var audioDevice in devices)
            {
                deviceList.Add(new
                {
                    name = audioDevice.FriendlyName,
                    id = audioDevice.ID,
                    state = audioDevice.State.ToString(),
                    isDefault = audioDevice.ID == device.ID
                });
            }
            
            info["availableDevices"] = deviceList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audio device info");
            info["error"] = ex.Message;
        }
        
        return info;
    }

    public void Dispose()
    {
        _deviceEnumerator?.Dispose();
    }
}
