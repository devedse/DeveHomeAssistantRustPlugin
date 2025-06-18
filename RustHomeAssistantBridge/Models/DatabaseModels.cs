using System.ComponentModel.DataAnnotations;

namespace RustHomeAssistantBridge.Models;

public class RustServer
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ServerAddress { get; set; } = string.Empty;
    
    [Required]
    public int Port { get; set; }
    
    [Required]
    public ulong PlayerId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string PlayerToken { get; set; } = string.Empty;
    
    public bool UseFacepunchProxy { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastConnectedAt { get; set; }
    
    public string? LastError { get; set; }
    
    // Navigation properties
    public virtual ICollection<ServerLog> Logs { get; set; } = new List<ServerLog>();
    public virtual ICollection<EntityInfo> Entities { get; set; } = new List<EntityInfo>();
}

public class EntityInfo
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ServerId { get; set; }
    
    [Required]
    public uint EntityId { get; set; }
    
    [MaxLength(50)]
    public string EntityType { get; set; } = "Unknown";
    
    public bool IsActive { get; set; }
    
    public int? Capacity { get; set; }
    
    public bool HasProtection { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual RustServer Server { get; set; } = null!;
}

public class ServerLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ServerId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public string? Data { get; set; } // JSON data
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual RustServer Server { get; set; } = null!;
}

public class FcmCredential
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string CredentialsJson { get; set; } = string.Empty;
    
    public string? NotificationIds { get; set; } // Comma-separated list
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsed { get; set; }
}
