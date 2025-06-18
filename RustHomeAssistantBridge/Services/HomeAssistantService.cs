using Microsoft.Extensions.Options;
using RustHomeAssistantBridge.Configuration;
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
        //_httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
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

    public async Task UpdateEntity(dynamic entityInfo)
    {
        try
        {
            var payload = new
            {
                entity_type = "generic_entity",
                entity_id = entityInfo.entity_id,
                is_active = entityInfo.is_active,
                capacity = entityInfo.capacity,
                has_protection = entityInfo.has_protection,
                timestamp = entityInfo.timestamp
            };

            await SendWebhook("rust_entity_update", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update entity in Home Assistant");
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

    public async Task SendServerPairingNotification(dynamic pairingInfo)
    {
        try
        {
            var payload = new
            {
                event_type = pairingInfo.event_type,
                server_name = pairingInfo.server_info?.Name ?? "Unknown",
                server_ip = pairingInfo.server_info?.Ip ?? "Unknown",
                server_port = pairingInfo.server_info?.Port ?? 0,
                player_id = pairingInfo.server_info?.PlayerId ?? 0,
                timestamp = pairingInfo.timestamp
            };

            await SendWebhook("rust_server_pairing", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send server pairing notification to Home Assistant");
        }
    }

    public async Task SendEntityPairingNotification(dynamic pairingInfo)
    {
        try
        {
            var payload = new
            {
                event_type = pairingInfo.event_type,
                entity_id = pairingInfo.entity_info?.EntityId ?? 0,
                entity_type = pairingInfo.entity_info?.EntityType ?? "Unknown",
                entity_name = pairingInfo.entity_info?.EntityName ?? "Unknown",
                timestamp = pairingInfo.timestamp
            };

            await SendWebhook("rust_entity_pairing", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send entity pairing notification to Home Assistant");
        }
    }

    public async Task SendAlarmNotification(dynamic alarmInfo)
    {
        try
        {
            var payload = new
            {
                event_type = alarmInfo.event_type,
                alarm_message = alarmInfo.alarm_info?.Message ?? "Alarm triggered",
                entity_id = alarmInfo.alarm_info?.EntityId ?? 0,
                timestamp = alarmInfo.timestamp
            };

            await SendWebhook("rust_alarm_triggered", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alarm notification to Home Assistant");
        }
    }

    public async Task SendGeneralNotification(dynamic notificationInfo)
    {
        try
        {
            var payload = new
            {
                event_type = notificationInfo.event_type,
                notification_data = notificationInfo.notification,
                timestamp = notificationInfo.timestamp
            };

            await SendWebhook("rust_general_notification", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send general notification to Home Assistant");
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
