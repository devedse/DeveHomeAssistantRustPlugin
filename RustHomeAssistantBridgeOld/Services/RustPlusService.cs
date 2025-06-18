using Microsoft.Extensions.Logging;
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
        {
            // Initialize RustPlus client
            _rustPlusClient = new RustPlus(
                _config.Server,
                _config.Port,
                _config.PlayerId,
                int.Parse(_config.PlayerToken), // Player token as int
                _config.UseFacepunchProxy);

            // Subscribe to basic events
            _rustPlusClient.Connected += OnConnected;
            _rustPlusClient.Disconnected += OnDisconnected;
            _rustPlusClient.ErrorOccurred += OnErrorOccurred;

            // Connect to Rust+ server
            await _rustPlusClient.ConnectAsync();
            
            _logger.LogInformation("Connected to Rust+ server {Server}:{Port}", _config.Server, _config.Port);

            // Get initial server info
            var serverInfo = await _rustPlusClient.GetInfoAsync();
            if (serverInfo.IsSuccess && serverInfo.Data != null)
            {
                _logger.LogInformation("Server: {ServerName}, Players: {PlayerCount}", 
                    serverInfo.Data.Name, 
                    serverInfo.Data.Size); // Using Size instead of Players

                // Send server info to Home Assistant
                await _homeAssistantService.SendServerInfo(serverInfo.Data);
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
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        _logger.LogInformation("Rust+ client connected");
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning("Rust+ client disconnected");
    }

    private void OnErrorOccurred(object? sender, Exception e)
    {
        _logger.LogError(e, "Rust+ client error occurred");
    }

    private async Task PeriodicMonitoring(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _rustPlusClient != null)
        {
            try
            {
                // Get server info periodically
                var serverInfo = await _rustPlusClient.GetInfoAsync();
                if (serverInfo.IsSuccess && serverInfo.Data != null)
                {
                    await _homeAssistantService.SendServerInfo(serverInfo.Data);
                }

                // Get time info
                var timeInfo = await _rustPlusClient.GetTimeAsync();
                if (timeInfo.IsSuccess && timeInfo.Data != null)
                {
                    _logger.LogDebug("Game time: {Time}", timeInfo.Data.Time);
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

    public async Task<bool> GetEntityInfo(uint entityId)
    {
        if (_rustPlusClient == null)
        {
            _logger.LogWarning("Rust+ client not connected");
            return false;
        }

        try
        {
            var entityInfo = await _rustPlusClient.GetEntityInfoAsync(entityId);
            if (entityInfo.IsSuccess && entityInfo.Data != null)
            {
                _logger.LogInformation("Entity {EntityId} - Active: {IsActive}, Capacity: {Capacity}", 
                    entityId, entityInfo.Data.IsActive, entityInfo.Data.Capacity);
                
                // Send to Home Assistant based on entity type
                dynamic payload = new
                {
                    entity_id = entityId,
                    is_active = entityInfo.Data.IsActive,
                    capacity = entityInfo.Data.Capacity,
                    has_protection = entityInfo.Data.HasProtection,
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

    public async Task<bool> SetEntityValue(uint entityId, bool value)
    {
        if (_rustPlusClient == null)
        {
            _logger.LogWarning("Rust+ client not connected");
            return false;
        }

        try
        {
            var result = await _rustPlusClient.SetEntityValueAsync(entityId, value);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Entity {EntityId} set to {Value}", entityId, value);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to set entity {EntityId} to {Value}: {Error}", 
                    entityId, value, result.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting entity {EntityId} to {Value}", entityId, value);
            return false;
        }
    }
}
