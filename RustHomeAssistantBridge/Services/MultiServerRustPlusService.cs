using Microsoft.EntityFrameworkCore;
using RustHomeAssistantBridge.Data;
using RustHomeAssistantBridge.Models;
using RustPlusApi;
using System.Collections.Concurrent;
using System.Text.Json;

namespace RustHomeAssistantBridge.Services;

public class RustServerConnection
{
    public RustServer ServerInfo { get; set; } = null!;
    public RustPlus Client { get; set; } = null!;
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    public bool IsConnected { get; set; }
}

public class MultiServerRustPlusService
{
    private readonly ILogger<MultiServerRustPlusService> _logger;
    private readonly HomeAssistantService _homeAssistantService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<int, RustServerConnection> _connections = new();
    private readonly Timer _monitoringTimer;

    public MultiServerRustPlusService(
        ILogger<MultiServerRustPlusService> logger,
        HomeAssistantService homeAssistantService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _homeAssistantService = homeAssistantService;
        _scopeFactory = scopeFactory;
        
        // Timer to periodically check connections and update server info
        _monitoringTimer = new Timer(MonitorConnections, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Multi-Server Rust+ service...");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();

        // Get all active servers from database
        var activeServers = await dbContext.RustServers
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var server in activeServers)
        {
            try
            {
                await ConnectToServer(server);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to server {ServerName} ({ServerAddress}:{Port})", 
                    server.Name, server.ServerAddress, server.Port);
                await LogServerEvent(server.Id, "ConnectionError", ex.Message);
            }
        }

        _logger.LogInformation("Multi-Server Rust+ service started with {ServerCount} servers", activeServers.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Multi-Server Rust+ service...");

        _monitoringTimer?.Dispose();

        var disconnectTasks = _connections.Values.Select(async connection =>
        {
            try
            {
                connection.CancellationTokenSource.Cancel();
                await connection.Client.DisconnectAsync();
                connection.IsConnected = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from server {ServerName}", connection.ServerInfo.Name);
            }
        });

        await Task.WhenAll(disconnectTasks);
        _connections.Clear();

        _logger.LogInformation("Multi-Server Rust+ service stopped");
    }

    public async Task<bool> AddServer(RustServer server)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();

        try
        {
            dbContext.RustServers.Add(server);
            await dbContext.SaveChangesAsync();

            if (server.IsActive)
            {
                await ConnectToServer(server);
            }

            _logger.LogInformation("Added new server {ServerName} ({ServerAddress}:{Port})", 
                server.Name, server.ServerAddress, server.Port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add server {ServerName}", server.Name);
            return false;
        }
    }

    public async Task<bool> RemoveServer(int serverId)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();

        try
        {
            // Disconnect if connected
            if (_connections.TryRemove(serverId, out var connection))
            {
                connection.CancellationTokenSource.Cancel();
                await connection.Client.DisconnectAsync();
            }

            // Remove from database
            var server = await dbContext.RustServers.FindAsync(serverId);
            if (server != null)
            {
                dbContext.RustServers.Remove(server);
                await dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Removed server {ServerName}", server.Name);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove server {ServerId}", serverId);
            return false;
        }
    }

    public async Task<List<RustServer>> GetServers()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();

        return await dbContext.RustServers
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<bool> GetEntityInfo(int serverId, uint entityId)
    {
        if (!_connections.TryGetValue(serverId, out var connection) || !connection.IsConnected)
        {
            _logger.LogWarning("Server {ServerId} not connected", serverId);
            return false;
        }

        try
        {
            var entityInfo = await connection.Client.GetEntityInfoLegacyAsync(entityId);
            if (entityInfo != null)
            {
                await UpdateEntityInfo(serverId, entityId, entityInfo);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity info for server {ServerId}, entity {EntityId}", serverId, entityId);
            return false;
        }
    }

    public async Task<bool> SendTeamMessage(int serverId, string message)
    {
        if (!_connections.TryGetValue(serverId, out var connection) || !connection.IsConnected)
        {
            _logger.LogWarning("Server {ServerId} not connected", serverId);
            return false;
        }

        try
        {
            var result = await connection.Client.SendTeamMessageAsync(message);
            if (result.IsSuccess)
            {
                await LogServerEvent(serverId, "TeamMessageSent", message);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending team message to server {ServerId}", serverId);
            return false;
        }
    }

    private async Task ConnectToServer(RustServer server)
    {
        try
        {
            var client = new RustPlus(
                server.ServerAddress,
                server.Port,
                server.PlayerId,
                int.Parse(server.PlayerToken),
                server.UseFacepunchProxy);

            var connection = new RustServerConnection
            {
                ServerInfo = server,
                Client = client
            };

            // Subscribe to events
            client.Connected += async (sender, _) => await OnServerConnected(connection);
            client.Disconnected += async (sender, _) => await OnServerDisconnected(connection);
            client.ErrorOccurred += async (sender, ex) => await OnServerError(connection, ex);
            client.OnSmartSwitchTriggered += async (sender, data) => await OnSmartSwitchTriggered(connection, data);
            client.OnStorageMonitorTriggered += async (sender, data) => await OnStorageMonitorTriggered(connection, data);
            client.OnTeamChatReceived += async (sender, data) => await OnTeamChatReceived(connection, data);

            await client.ConnectAsync();
            
            _connections.TryAdd(server.Id, connection);
            
            _logger.LogInformation("Connected to server {ServerName} ({ServerAddress}:{Port})", 
                server.Name, server.ServerAddress, server.Port);

            // Start monitoring for this server
            _ = Task.Run(() => MonitorServer(connection), connection.CancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to server {ServerName}", server.Name);
            await LogServerEvent(server.Id, "ConnectionError", ex.Message);
            throw;
        }
    }

    private async Task OnServerConnected(RustServerConnection connection)
    {
        connection.IsConnected = true;
        connection.LastHeartbeat = DateTime.UtcNow;
        
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();
        
        var server = await dbContext.RustServers.FindAsync(connection.ServerInfo.Id);
        if (server != null)
        {
            server.LastConnectedAt = DateTime.UtcNow;
            server.LastError = null;
            await dbContext.SaveChangesAsync();
        }

        await LogServerEvent(connection.ServerInfo.Id, "Connected", "Server connected successfully");
        
        // Get initial server info
        try
        {
            var serverInfo = await connection.Client.GetInfoAsync();
            if (serverInfo.IsSuccess && serverInfo.Data != null)
            {
                await _homeAssistantService.SendServerInfo(serverInfo.Data);
                await LogServerEvent(connection.ServerInfo.Id, "ServerInfo", JsonSerializer.Serialize(serverInfo.Data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting initial server info for {ServerName}", connection.ServerInfo.Name);
        }
    }

    private async Task OnServerDisconnected(RustServerConnection connection)
    {
        connection.IsConnected = false;
        await LogServerEvent(connection.ServerInfo.Id, "Disconnected", "Server disconnected");
    }

    private async Task OnServerError(RustServerConnection connection, Exception ex)
    {
        connection.IsConnected = false;
        await LogServerEvent(connection.ServerInfo.Id, "Error", ex.Message);
        
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();
        
        var server = await dbContext.RustServers.FindAsync(connection.ServerInfo.Id);
        if (server != null)
        {
            server.LastError = ex.Message;
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task OnSmartSwitchTriggered(RustServerConnection connection, object data)
    {
        await _homeAssistantService.UpdateSmartSwitch(data);
        await LogServerEvent(connection.ServerInfo.Id, "SmartSwitchTriggered", JsonSerializer.Serialize(data));
    }

    private async Task OnStorageMonitorTriggered(RustServerConnection connection, object data)
    {
        await _homeAssistantService.UpdateStorageMonitor(data);
        await LogServerEvent(connection.ServerInfo.Id, "StorageMonitorTriggered", JsonSerializer.Serialize(data));
    }

    private async Task OnTeamChatReceived(RustServerConnection connection, object data)
    {
        await _homeAssistantService.SendTeamChatMessage(data);
        await LogServerEvent(connection.ServerInfo.Id, "TeamChatReceived", JsonSerializer.Serialize(data));
    }

    private async Task MonitorServer(RustServerConnection connection)
    {
        while (!connection.CancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                if (connection.IsConnected)
                {
                    // Get server info periodically
                    var serverInfo = await connection.Client.GetInfoAsync();
                    if (serverInfo.IsSuccess && serverInfo.Data != null)
                    {
                        await _homeAssistantService.SendServerInfo(serverInfo.Data);
                        connection.LastHeartbeat = DateTime.UtcNow;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), connection.CancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring server {ServerName}", connection.ServerInfo.Name);
                await Task.Delay(TimeSpan.FromSeconds(60), connection.CancellationTokenSource.Token);
            }
        }
    }

    private async void MonitorConnections(object? state)
    {
        foreach (var kvp in _connections)
        {
            var connection = kvp.Value;
            
            // Check if connection is stale
            if (DateTime.UtcNow - connection.LastHeartbeat > TimeSpan.FromMinutes(5))
            {
                _logger.LogWarning("Connection to server {ServerName} appears stale, attempting reconnect", 
                    connection.ServerInfo.Name);
                
                try
                {
                    await connection.Client.DisconnectAsync();
                    await ConnectToServer(connection.ServerInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconnect to server {ServerName}", connection.ServerInfo.Name);
                }
            }
        }
    }

    private async Task UpdateEntityInfo(int serverId, uint entityId, object entityInfo)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();

        var entity = await dbContext.EntityInfos
            .FirstOrDefaultAsync(e => e.ServerId == serverId && e.EntityId == entityId);

        if (entity == null)
        {
            entity = new EntityInfo
            {
                ServerId = serverId,
                EntityId = entityId
            };
            dbContext.EntityInfos.Add(entity);
        }

        entity.LastUpdated = DateTime.UtcNow;
        // Update other properties based on entityInfo structure
        
        await dbContext.SaveChangesAsync();
        
        dynamic payload = new
        {
            server_id = serverId,
            entity_id = entityId,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        await _homeAssistantService.UpdateEntity(payload);
    }

    private async Task LogServerEvent(int serverId, string eventType, string message, string? data = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();

        var log = new ServerLog
        {
            ServerId = serverId,
            EventType = eventType,
            Message = message,
            Data = data
        };

        dbContext.ServerLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        foreach (var connection in _connections.Values)
        {
            connection.CancellationTokenSource?.Dispose();
        }
    }
}
