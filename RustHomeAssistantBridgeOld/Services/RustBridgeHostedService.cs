using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RustHomeAssistantBridge.Services;

public class RustBridgeHostedService : BackgroundService
{
    private readonly ILogger<RustBridgeHostedService> _logger;
    private readonly RustPlusService _rustPlusService;

    public RustBridgeHostedService(
        ILogger<RustBridgeHostedService> logger,
        RustPlusService rustPlusService)
    {
        _logger = logger;
        _rustPlusService = rustPlusService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rust Home Assistant Bridge service starting...");

        try
        {
            await _rustPlusService.StartAsync(stoppingToken);

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
            _logger.LogInformation("Rust Home Assistant Bridge service stopped");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rust Home Assistant Bridge service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}
