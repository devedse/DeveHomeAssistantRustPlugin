using Microsoft.Extensions.Options;
using RustHomeAssistantBridge.Configuration;
using RustHomeAssistantBridge.Models;
using RustPlusApi.Fcm;
using RustPlusApi.Fcm.Data;
using System.Text.Json;

namespace RustHomeAssistantBridge.Services;

/// <summary>
/// Simple FCM listener service for server pairing notifications
/// </summary>
public class FcmListenerService : BackgroundService
{
    private readonly ILogger<FcmListenerService> _logger;
    private readonly HomeAssistantService _homeAssistantService;
    private readonly FcmConfig _fcmConfig;
    private RustPlusFcmListener? _listener;

    public FcmListenerService(
        ILogger<FcmListenerService> logger,
        HomeAssistantService homeAssistantService,
        IOptions<FcmConfig> fcmConfig)
    {
        _logger = logger;
        _homeAssistantService = homeAssistantService;
        _fcmConfig = fcmConfig.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_fcmConfig.Enabled)
        {
            _logger.LogInformation("FCM listener is disabled in configuration");
            return;
        }

        _logger.LogInformation("Starting FCM listener service...");

        try
        {
            // Load credentials and start listener
            var credentials = LoadFcmCredentials();
            if (credentials == null)
            {
                _logger.LogError("Failed to load FCM credentials");
                return;
            }

            _listener = new RustPlusFcmListener(credentials);

            // Basic event handlers
            _listener.Connected += (_, _) => _logger.LogInformation("[FCM CONNECTED]: {DateTime}", DateTime.Now);
            _listener.Disconnected += (_, _) => _logger.LogInformation("[FCM DISCONNECTED]: {DateTime}", DateTime.Now);
            _listener.ErrorOccurred += (_, error) => _logger.LogError("[FCM ERROR]: {Error}", error);

            // Server pairing event
            _listener.OnServerPairing += async (_, pairing) =>
            {
                _logger.LogInformation("[SERVER PAIRING]: {Pairing}", JsonSerializer.Serialize(pairing, new JsonSerializerOptions { WriteIndented = true }));
                
                var pairingInfo = new
                {
                    event_type = "server_pairing",
                    server_name = pairing.Data?.Name ?? "Unknown",
                    server_ip = pairing.Data?.Ip ?? "Unknown", 
                    server_port = pairing.Data?.Port ?? 0,
                    timestamp = DateTime.UtcNow
                };

                await _homeAssistantService.SendServerPairingNotification(pairingInfo);
            };

            await _listener.ConnectAsync();
            _logger.LogInformation("FCM listener connected successfully");

            // Keep running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM listener service error");
        }
        finally
        {
            _listener?.Disconnect();
            _logger.LogInformation("FCM listener service stopped");
        }
    }

    private Credentials? LoadFcmCredentials()
    {
        try
        {
            var fullPath = Path.IsPathRooted(_fcmConfig.CredentialsPath)
                ? _fcmConfig.CredentialsPath
                : Path.Combine(AppContext.BaseDirectory, _fcmConfig.CredentialsPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogError("FCM credentials file not found: {FullPath}", fullPath);
                return null;
            }

            var json = File.ReadAllText(fullPath);
            var config = JsonSerializer.Deserialize<RustPlusConfig>(json);

            if (config?.FcmCredentials?.Gcm == null)
                return null;

            var retval = new Credentials
            {
                Keys = new Keys
                {
                    PrivateKey = "",
                    PublicKey = "",
                    AuthSecret = ""
                },
                Gcm = new Gcm
                {
                    AndroidId = ulong.Parse(config.FcmCredentials.Gcm.AndroidId),
                    SecurityToken = ulong.Parse(config.FcmCredentials.Gcm.SecurityToken)
                }
            };

            return retval;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FCM credentials");
            return null;
        }
    }
}
