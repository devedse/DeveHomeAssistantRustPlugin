namespace RustHomeAssistantBridge.Services;

public class MultiServerRustBridgeHostedService : BackgroundService
{
    private readonly ILogger<MultiServerRustBridgeHostedService> _logger;
    private readonly MultiServerRustPlusService _multiServerService;

    public MultiServerRustBridgeHostedService(
        ILogger<MultiServerRustBridgeHostedService> logger,
        MultiServerRustPlusService multiServerService)
    {
        _logger = logger;
        _multiServerService = multiServerService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Multi-Server Rust Home Assistant Bridge service starting...");

        try
        {
            await _multiServerService.StartAsync(stoppingToken);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Multi-Server Rust Home Assistant Bridge service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multi-Server Rust Home Assistant Bridge service encountered an error");
            throw;
        }
        finally
        {
            await _multiServerService.StopAsync(stoppingToken);
            _logger.LogInformation("Multi-Server Rust Home Assistant Bridge service stopped");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Multi-Server Rust Home Assistant Bridge service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}
