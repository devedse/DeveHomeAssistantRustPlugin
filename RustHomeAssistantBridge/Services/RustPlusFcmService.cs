using RustHomeAssistantBridge.Configuration;
using RustPlusApi.Fcm;
using RustPlusApi.Fcm.Data;

namespace RustHomeAssistantBridge.Services;

public class RustPlusFcmService
{
    private readonly ILogger<RustPlusFcmService> _logger;
    private readonly HomeAssistantService _homeAssistantService;
    private RustPlusFcmListener? _fcmListener;

    public RustPlusFcmService(
        ILogger<RustPlusFcmService> logger,
        HomeAssistantService homeAssistantService)
    {
        _logger = logger;
        _homeAssistantService = homeAssistantService;
    }

    public async Task StartAsync(Credentials credentials, List<string> notificationIds, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Rust+ FCM service...");

        try
        {
            // Initialize FCM listener
            _fcmListener = new RustPlusFcmListener(credentials, notificationIds);

            // Subscribe to events with generic object handlers to avoid type issues
            _fcmListener.OnServerPairing += OnServerPairing;
            _fcmListener.OnEntityParing += OnEntityPairing;
            _fcmListener.OnAlarmTriggered += OnAlarmTriggered;

            // Connect to FCM
            await _fcmListener.ConnectAsync();
            
            _logger.LogInformation("Connected to FCM service for Rust+ notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Rust+ FCM service");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Rust+ FCM service...");

        if (_fcmListener != null)
        {
            _fcmListener.Disconnect();
            _fcmListener = null;
        }

        return Task.CompletedTask;
    }

    private async void OnServerPairing(object? sender, object e)
    {
        _logger.LogInformation("Server pairing notification received: {Data}", e);
        
        try
        {
            var payload = new
            {
                event_type = "server_pairing",
                server_info = e,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await _homeAssistantService.SendServerPairingNotification(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing server pairing notification");
        }
    }

    private async void OnEntityPairing(object? sender, object e)
    {
        _logger.LogInformation("Entity pairing notification received: {Data}", e);
        
        try
        {
            var payload = new
            {
                event_type = "entity_pairing", 
                entity_info = e,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await _homeAssistantService.SendEntityPairingNotification(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing entity pairing notification");
        }
    }

    private async void OnAlarmTriggered(object? sender, object e)
    {
        _logger.LogInformation("Alarm triggered notification received: {Data}", e);
        
        try
        {
            var payload = new
            {
                event_type = "alarm_triggered",
                alarm_info = e,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await _homeAssistantService.SendAlarmNotification(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing alarm notification");
        }
    }
}
