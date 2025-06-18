using Microsoft.Extensions.Options;
using RustHomeAssistantBridge.Configuration;

namespace RustHomeAssistantBridge.Services;

public class RustBridgeHostedService : BackgroundService
{
    private readonly ILogger<RustBridgeHostedService> _logger;
    private readonly RustPlusService _rustPlusService;
    private readonly RustPlusFcmService _fcmService;
    private readonly FcmConfig _fcmConfig;

    public RustBridgeHostedService(
        ILogger<RustBridgeHostedService> logger,
        RustPlusService rustPlusService,
        RustPlusFcmService fcmService,
        IOptions<FcmConfig> fcmConfig)
    {
        _logger = logger;
        _rustPlusService = rustPlusService;
        _fcmService = fcmService;
        _fcmConfig = fcmConfig.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rust Home Assistant Bridge service starting...");

        try
        {
            // Start Rust+ service
            await _rustPlusService.StartAsync(stoppingToken);

            // Start FCM service if enabled and credentials are available
            if (_fcmConfig.Enabled && !string.IsNullOrEmpty(_fcmConfig.CredentialsPath))
            {
                try
                {
                    // Load FCM credentials (this would need to be implemented based on the credential format)
                    var credentials = LoadFcmCredentials(_fcmConfig.CredentialsPath);
                    await _fcmService.StartAsync(credentials, _fcmConfig.NotificationIds, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start FCM service, continuing without notifications");
                }
            }
            else
            {
                _logger.LogInformation("FCM service disabled or credentials not configured");
            }

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Rust Home Assistant Bridge service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rust Home Assistant Bridge service encountered an error");
            throw;
        }
        finally
        {
            await _rustPlusService.StopAsync(stoppingToken);
            await _fcmService.StopAsync(stoppingToken);
            _logger.LogInformation("Rust Home Assistant Bridge service stopped");
        }
    }    private object LoadFcmCredentials(string credentialsPath)
    {
        // TODO: Implement credential loading based on the format required by RustPlusApi.Fcm
        // This will depend on how the credentials are stored (JSON file, etc.)
        // For now, return a placeholder object since we don't have real credentials
        _logger.LogWarning("FCM credential loading not yet implemented. Using placeholder.");
        
        // This is a placeholder - you would need to load actual FCM credentials here
        // The credentials format depends on the RustPlusApi.Fcm.Data.Credentials class
        throw new NotImplementedException("FCM credential loading needs to be implemented based on your credential format. Please refer to the RustPlusApi.Fcm documentation for the correct Credentials format.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rust Home Assistant Bridge service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}
