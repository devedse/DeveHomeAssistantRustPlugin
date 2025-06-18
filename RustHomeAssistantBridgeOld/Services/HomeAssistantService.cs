using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RustHomeAssistantBridge.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace RustHomeAssistantBridge.Services;

public class HomeAssistantService
{
    private readonly ILogger<HomeAssistantService> _logger;
    private readonly HomeAssistantConfig _config;
    private readonly HttpClient _httpClient;

    public HomeAssistantService(
        ILogger<HomeAssistantService> logger,
        IOptions<HomeAssistantConfig> config,
        HttpClient httpClient)
    {
        _logger = logger;
        _config = config.Value;
        _httpClient = httpClient;
        
        // Set up HTTP client
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AccessToken}");
        _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
    }

    public async Task SendServerInfo(dynamic serverInfo)
    {
        try
        {
            var payload = new
            {
                server_name = serverInfo.Name ?? "Unknown",
                player_count = serverInfo.Players ?? 0,
                max_players = serverInfo.MaxPlayers ?? 0,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await SendWebhook("rust_server_info", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send server info to Home Assistant");
        }
    }

    public async Task UpdateSmartSwitch(dynamic switchInfo)
    {
        try
        {
            var payload = new
            {
                entity_type = "smart_switch",
                is_active = switchInfo.IsActive,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await SendWebhook("rust_smart_switch", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update smart switch in Home Assistant");
        }
    }

    public async Task UpdateStorageMonitor(dynamic storageInfo)
    {
        try
        {
            var payload = new
            {
                entity_type = "storage_monitor",
                has_items = storageInfo.HasItems,
                item_count = storageInfo.Items?.Count ?? 0,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await SendWebhook("rust_storage_monitor", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update storage monitor in Home Assistant");
        }
    }

    public async Task SendTeamChatMessage(dynamic chatInfo)
    {
        try
        {
            var payload = new
            {
                entity_type = "team_chat",
                player_name = chatInfo.PlayerName ?? "Unknown",
                message = chatInfo.Message ?? "",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await SendWebhook("rust_team_chat", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send team chat to Home Assistant");
        }
    }

    private async Task SendWebhook(string eventType, object payload)
    {
        try
        {
            var webhookData = new
            {
                type = eventType,
                data = payload
            };

            var json = JsonSerializer.Serialize(webhookData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_config.BaseUrl}/api/webhook/{_config.WebhookId}";
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully sent webhook {EventType} to Home Assistant", eventType);
            }
            else
            {
                _logger.LogWarning("Failed to send webhook {EventType} to Home Assistant. Status: {StatusCode}", 
                    eventType, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook {EventType} to Home Assistant", eventType);
        }
    }
}
