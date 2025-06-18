using Microsoft.Extensions.Options;
using RustHomeAssistantBridge.Configuration;
using RustPlusApi;

namespace RustHomeAssistantBridge.Services;

public class RustPlusService
{
    private readonly ILogger<RustPlusService> _logger;
    private readonly RustPlusConfig _config;
    private readonly HomeAssistantService _homeAssistantService;
    private RustPlus? _rustPlusClient;

    public RustPlusService(
        ILogger<RustPlusService> logger,
        IOptions<RustPlusConfig> config,
        HomeAssistantService homeAssistantService)
    {
        _logger = logger;
        _config = config.Value;
        _homeAssistantService = homeAssistantService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Rust+ service...");

        try
        {            // Initialize RustPlus client with proper parameters from README
            _rustPlusClient = new RustPlus(
                _config.Server,
                _config.Port,
                _config.PlayerId,
                int.Parse(_config.PlayerToken), // Convert string to int
                _config.UseFacepunchProxy);

            // Subscribe to basic events as shown in README
            _rustPlusClient.Connecting += (sender, _) => _logger.LogInformation("Rust+ client connecting...");
            _rustPlusClient.Connected += (sender, _) => _logger.LogInformation("Rust+ client connected");
            _rustPlusClient.Disconnecting += (sender, _) => _logger.LogInformation("Rust+ client disconnecting...");
            _rustPlusClient.Disconnected += (sender, _) => _logger.LogWarning("Rust+ client disconnected");
            _rustPlusClient.ErrorOccurred += (sender, ex) => _logger.LogError(ex, "Rust+ client error occurred");

            // Subscribe to message events as shown in README
            _rustPlusClient.MessageReceived += (sender, message) => _logger.LogDebug("Message received from Rust+ server");
            _rustPlusClient.NotificationReceived += OnNotificationReceived;
            _rustPlusClient.ResponseReceived += (sender, message) => _logger.LogDebug("Response received from Rust+ server");

            // Subscribe to specific entity events as shown in README
            _rustPlusClient.OnSmartSwitchTriggered += OnSmartSwitchTriggered;
            _rustPlusClient.OnStorageMonitorTriggered += OnStorageMonitorTriggered;
            _rustPlusClient.OnTeamChatReceived += OnTeamChatReceived;

            // Connect to Rust+ server
            await _rustPlusClient.ConnectAsync();
            
            _logger.LogInformation("Connected to Rust+ server {Server}:{Port}", _config.Server, _config.Port);

            // Get initial server info using the new API as shown in README
            var serverInfoResponse = await _rustPlusClient.GetInfoAsync();
            if (serverInfoResponse.IsSuccess && serverInfoResponse.Data != null)
            {
                _logger.LogInformation("Server: {ServerName}", serverInfoResponse.Data.Name);
                await _homeAssistantService.SendServerInfo(serverInfoResponse.Data);
            }

            // Start periodic monitoring
            _ = Task.Run(() => PeriodicMonitoring(cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Rust+ service");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Rust+ service...");

        if (_rustPlusClient != null)
        {
            await _rustPlusClient.DisconnectAsync();
            _rustPlusClient = null;
        }
    }    private void OnNotificationReceived(object? sender, object message)
    {
        _logger.LogInformation("Notification received from Rust+ server");
        // Handle notifications that aren't direct responses to requests
        // These could be entity state changes, alarms, etc.
    }

    private async void OnSmartSwitchTriggered(object? sender, object smartSwitch)
    {
        _logger.LogInformation("Smart switch triggered");
        await _homeAssistantService.UpdateSmartSwitch(smartSwitch);
    }

    private async void OnStorageMonitorTriggered(object? sender, object storageMonitor)
    {
        _logger.LogInformation("Storage monitor triggered");
        await _homeAssistantService.UpdateStorageMonitor(storageMonitor);
    }

    private async void OnTeamChatReceived(object? sender, object message)
    {
        _logger.LogInformation("Team chat received");
        await _homeAssistantService.SendTeamChatMessage(message);
    }

    private async Task PeriodicMonitoring(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _rustPlusClient != null)
        {
            try
            {
                // Get server info periodically using new API
                var serverInfoResponse = await _rustPlusClient.GetInfoAsync();
                if (serverInfoResponse.IsSuccess && serverInfoResponse.Data != null)
                {
                    await _homeAssistantService.SendServerInfo(serverInfoResponse.Data);
                }

                // Get time info using new API
                var timeResponse = await _rustPlusClient.GetTimeAsync();
                if (timeResponse.IsSuccess && timeResponse.Data != null)
                {
                    _logger.LogDebug("Game time: {Time}", timeResponse.Data.Time);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic monitoring");
                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            }
        }
    }

    public async Task<bool> GetSmartSwitchInfo(uint entityId)
    {
        if (_rustPlusClient == null)
        {
            _logger.LogWarning("Rust+ client not connected");
            return false;
        }

        try
        {
            // Use GetSmartSwitchInfoAsync as shown in README
            var response = await _rustPlusClient.GetSmartSwitchInfoAsync(entityId);
            if (response.IsSuccess && response.Data != null)
            {
                _logger.LogInformation("Smart switch {EntityId} - Active: {IsActive}", 
                    entityId, response.Data.IsActive);
                
                await _homeAssistantService.UpdateSmartSwitch(response.Data);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to get smart switch info for {EntityId}: {Error}", 
                    entityId, response.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting smart switch info for {EntityId}", entityId);
            return false;
        }
    }

    public async Task<bool> SendTeamMessage(string message)
    {
        if (_rustPlusClient == null)
        {
            _logger.LogWarning("Rust+ client not connected");
            return false;
        }

        try
        {
            var response = await _rustPlusClient.SendTeamMessageAsync(message);
            if (response.IsSuccess)
            {
                _logger.LogInformation("Team message sent: {Message}", message);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send team message: {Error}", response.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending team message: {Message}", message);
            return false;
        }
    }

    public async Task<object?> GetMapInfo()
    {
        if (_rustPlusClient == null)
        {
            _logger.LogWarning("Rust+ client not connected");
            return null;
        }

        try
        {
            var response = await _rustPlusClient.GetMapAsync();
            if (response.IsSuccess && response.Data != null)
            {
                _logger.LogInformation("Map info retrieved successfully");
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to get map info: {Error}", response.Error?.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting map info");
            return null;
        }
    }

    public async Task<object?> GetTeamInfo()
    {
        if (_rustPlusClient == null)
        {
            _logger.LogWarning("Rust+ client not connected");
            return null;
        }

        try
        {
            var response = await _rustPlusClient.GetTeamInfoAsync();
            if (response.IsSuccess && response.Data != null)
            {
                _logger.LogInformation("Team info retrieved successfully");
                return response.Data;
            }
            else
            {
                _logger.LogWarning("Failed to get team info: {Error}", response.Error?.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team info");
            return null;
        }
    }

    // Method to get general entity info (could be any type of entity)
    public async Task<bool> GetEntityInfo(uint entityId)
    {
        if (_rustPlusClient == null)
        {
            _logger.LogWarning("Rust+ client not connected");
            return false;
        }

        try
        {
            // Use legacy method for general entity info
            var entityInfo = await _rustPlusClient.GetEntityInfoLegacyAsync(entityId);
            if (entityInfo != null)
            {
                _logger.LogInformation("Entity {EntityId} info retrieved", entityId);
                
                dynamic payload = new
                {
                    entity_id = entityId,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                await _homeAssistantService.UpdateEntity(payload);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity info for {EntityId}", entityId);
            return false;
        }
    }
}
