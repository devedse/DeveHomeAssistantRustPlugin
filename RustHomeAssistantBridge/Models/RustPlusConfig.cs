using System.Text.Json.Serialization;

namespace RustHomeAssistantBridge.Models;

public class RustPlusConfig
{
    [JsonPropertyName("fcm_credentials")]
    public FcmCredentials FcmCredentials { get; set; } = new();

    [JsonPropertyName("expo_push_token")]
    public string ExpoPushToken { get; set; } = string.Empty;

    [JsonPropertyName("rustplus_auth_token")]
    public string RustPlusAuthToken { get; set; } = string.Empty;
}

public class FcmCredentials
{
    [JsonPropertyName("gcm")]
    public GcmCredentials Gcm { get; set; } = new();

    [JsonPropertyName("fcm")]
    public FcmTokenCredentials Fcm { get; set; } = new();
}

public class GcmCredentials
{
    [JsonPropertyName("androidId")]
    public string AndroidId { get; set; } = string.Empty;

    [JsonPropertyName("securityToken")]
    public string SecurityToken { get; set; } = string.Empty;
}

public class FcmTokenCredentials
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
