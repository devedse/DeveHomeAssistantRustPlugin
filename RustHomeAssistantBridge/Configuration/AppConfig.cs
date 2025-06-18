namespace RustHomeAssistantBridge.Configuration;

public class RustPlusConfig
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public ulong PlayerId { get; set; }
    public string PlayerToken { get; set; } = string.Empty;
    public bool UseFacepunchProxy { get; set; } = false;
}

public class HomeAssistantConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookId { get; set; } = string.Empty;
}
