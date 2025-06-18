namespace RustHomeAssistantBridge.Configuration;

public class HomeAssistantConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookId { get; set; } = string.Empty;
}

public class FcmConfig
{
    public string CredentialsPath { get; set; } = string.Empty;
    public List<string> NotificationIds { get; set; } = new();
    public bool Enabled { get; set; } = false;
}
