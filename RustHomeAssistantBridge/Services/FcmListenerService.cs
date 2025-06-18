using Microsoft.Extensions.Options;
using RustHomeAssistantBridge.Configuration;
using RustHomeAssistantBridge.Models;
using RustPlusApi.Fcm;
using RustPlusApi.Fcm.Data;
using System.Security.Cryptography;
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
            _listener.Connecting += (_, _) => _logger.LogInformation("[FCM CONNECTING]: Attempting to connect to FCM...");
            _listener.Connected += (_, _) => _logger.LogInformation("[FCM CONNECTED]: {DateTime}", DateTime.Now);
            _listener.Disconnecting += (_, _) => _logger.LogInformation("[FCM DISCONNECTING]: Disconnecting from FCM...");
            _listener.Disconnected += (_, _) => _logger.LogInformation("[FCM DISCONNECTED]: {DateTime}", DateTime.Now);
            _listener.ErrorOccurred += (_, error) => _logger.LogError("[FCM ERROR]: {Error}", error);
            _listener.SocketClosed += (_, _) => _logger.LogInformation("[SOCKET CLOSED]: FCM socket closed");

            _listener.OnEntityParing += (_, _) => _logger.LogInformation("[ENTITY PAIRING]: Entity pairing event received");
            _listener.OnAlarmTriggered += (_, _) => _logger.LogInformation("[ALARM TRIGGERED]: Alarm triggered event received");
            _listener.OnStorageMonitorParing += (_, _) => _logger.LogInformation("[STORAGE MONITOR PAIRING]: Storage monitor pairing event received");
            _listener.OnParing += (_, _) => _logger.LogInformation("[PAIRING]: Pairing event received");

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
                return null;            // Generate FCM keys similar to the JavaScript implementation
            var keys = CreateKeys();

            var retval = new Credentials
            {
                Keys = new Keys
                {
                    PrivateKey = keys.PrivateKey,
                    PublicKey = keys.PublicKey,
                    AuthSecret = keys.AuthSecret
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

    private (string PrivateKey, string PublicKey, string AuthSecret) CreateKeys()
    {
        try
        {
            // Create ECDH using prime256v1 curve (same as Node.js crypto.createECDH('prime256v1'))
            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            
            // Get the public and private keys
            var privateKeyBytes = ecdh.ExportECPrivateKey();
            var publicKeyBytes = ecdh.ExportSubjectPublicKeyInfo();
            
            // Generate 16 random bytes for auth secret (same as crypto.randomBytes(16))
            var authSecretBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(authSecretBytes);
            
            // Convert to base64 and apply URL-safe encoding (replace chars like in JS)
            var privateKey = EscapeBase64(Convert.ToBase64String(privateKeyBytes));
            var publicKey = EscapeBase64(Convert.ToBase64String(publicKeyBytes));
            var authSecret = EscapeBase64(Convert.ToBase64String(authSecretBytes));
            
            return (privateKey, publicKey, authSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating FCM keys");
            // Return empty keys as fallback
            return ("", "", "");
        }
    }
    
    private static string EscapeBase64(string base64)
    {
        // Apply the same transformations as the JavaScript code
        return base64
            .Replace("=", "")
            .Replace("+", "-")
            .Replace("/", "_");
    }
}
