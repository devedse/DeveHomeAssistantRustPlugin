using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RustHomeAssistantBridge.Configuration;
using RustHomeAssistantBridge.Services;

namespace RustHomeAssistantBridge;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("secrets.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Create host builder
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<RustPlusConfig>(configuration.GetSection("RustPlus"));
                services.Configure<HomeAssistantConfig>(configuration.GetSection("HomeAssistant"));

                // HTTP Client
                services.AddHttpClient<HomeAssistantService>();

                // Services
                services.AddSingleton<HomeAssistantService>();
                services.AddSingleton<RustPlusService>();
                services.AddHostedService<RustBridgeHostedService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            })
            .Build();

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Application terminated unexpectedly");
        }
    }
}
