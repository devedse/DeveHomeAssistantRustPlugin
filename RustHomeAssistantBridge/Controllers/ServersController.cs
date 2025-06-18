using Microsoft.AspNetCore.Mvc;
using RustHomeAssistantBridge.Models;
using RustHomeAssistantBridge.Services;

namespace RustHomeAssistantBridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServersController : ControllerBase
{
    private readonly ILogger<ServersController> _logger;
    private readonly MultiServerRustPlusService _multiServerService;

    public ServersController(ILogger<ServersController> logger, MultiServerRustPlusService multiServerService)
    {
        _logger = logger;
        _multiServerService = multiServerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetServers()
    {
        try
        {
            var servers = await _multiServerService.GetServers();
            return Ok(servers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting servers");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddServer([FromBody] AddServerRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || 
                string.IsNullOrWhiteSpace(request.ServerAddress) ||
                string.IsNullOrWhiteSpace(request.PlayerToken))
            {
                return BadRequest(new { error = "Name, ServerAddress, and PlayerToken are required" });
            }

            var server = new RustServer
            {
                Name = request.Name,
                ServerAddress = request.ServerAddress,
                Port = request.Port,
                PlayerId = request.PlayerId,
                PlayerToken = request.PlayerToken,
                UseFacepunchProxy = request.UseFacepunchProxy,
                IsActive = request.IsActive
            };

            var success = await _multiServerService.AddServer(server);
            if (success)
            {
                return Ok(new { message = "Server added successfully", serverId = server.Id });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to add server" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding server");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{serverId}")]
    public async Task<IActionResult> RemoveServer(int serverId)
    {
        try
        {
            var success = await _multiServerService.RemoveServer(serverId);
            if (success)
            {
                return Ok(new { message = "Server removed successfully" });
            }
            else
            {
                return NotFound(new { error = "Server not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing server {ServerId}", serverId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{serverId}/entity/{entityId}")]
    public async Task<IActionResult> GetEntityInfo(int serverId, uint entityId)
    {
        try
        {
            var result = await _multiServerService.GetEntityInfo(serverId, entityId);
            if (result)
            {
                return Ok(new { message = $"Entity {entityId} info retrieved successfully for server {serverId}" });
            }
            else
            {
                return NotFound(new { error = $"Entity {entityId} not found or server {serverId} not connected" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity info for server {ServerId}, entity {EntityId}", serverId, entityId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("{serverId}/team-message")]
    public async Task<IActionResult> SendTeamMessage(int serverId, [FromBody] SendTeamMessageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            var result = await _multiServerService.SendTeamMessage(serverId, request.Message);
            if (result)
            {
                return Ok(new { message = "Team message sent successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to send team message or server not connected" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending team message to server {ServerId}", serverId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class AddServerRequest
{
    public string Name { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public ulong PlayerId { get; set; }
    public string PlayerToken { get; set; } = string.Empty;
    public bool UseFacepunchProxy { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class SendTeamMessageRequest
{
    public string Message { get; set; } = string.Empty;
}
