using Microsoft.AspNetCore.Mvc;
using RustHomeAssistantBridge.Services;

namespace RustHomeAssistantBridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RustPlusController : ControllerBase
{
    private readonly ILogger<RustPlusController> _logger;
    private readonly RustPlusService _rustPlusService;

    public RustPlusController(ILogger<RustPlusController> logger, RustPlusService rustPlusService)
    {
        _logger = logger;
        _rustPlusService = rustPlusService;
    }    [HttpGet("server-info")]
    public IActionResult GetServerInfo()
    {
        try
        {
            // This would ideally return cached server info or trigger a fresh request
            _logger.LogInformation("Server info requested via API");
            return Ok(new { message = "Server info request logged" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting server info");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }[HttpGet("entity/{entityId}")]
    public async Task<IActionResult> GetEntityInfo(uint entityId)
    {
        try
        {
            var result = await _rustPlusService.GetEntityInfo(entityId);
            if (result)
            {
                return Ok(new { message = $"Entity {entityId} info retrieved successfully" });
            }
            else
            {
                return NotFound(new { error = $"Entity {entityId} not found or could not be retrieved" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity info for {EntityId}", entityId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("smart-switch/{entityId}")]
    public async Task<IActionResult> GetSmartSwitchInfo(uint entityId)
    {
        try
        {
            var result = await _rustPlusService.GetSmartSwitchInfo(entityId);
            if (result)
            {
                return Ok(new { message = $"Smart switch {entityId} info retrieved successfully" });
            }
            else
            {
                return NotFound(new { error = $"Smart switch {entityId} not found or could not be retrieved" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting smart switch info for {EntityId}", entityId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("team-message")]
    public async Task<IActionResult> SendTeamMessage([FromBody] SendTeamMessageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            var result = await _rustPlusService.SendTeamMessage(request.Message);
            if (result)
            {
                return Ok(new { message = "Team message sent successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to send team message" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending team message");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("map")]
    public async Task<IActionResult> GetMapInfo()
    {
        try
        {
            var mapInfo = await _rustPlusService.GetMapInfo();
            if (mapInfo != null)
            {
                return Ok(mapInfo);
            }
            else
            {
                return NotFound(new { error = "Map info not available" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting map info");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("team")]
    public async Task<IActionResult> GetTeamInfo()
    {
        try
        {
            var teamInfo = await _rustPlusService.GetTeamInfo();
            if (teamInfo != null)
            {
                return Ok(teamInfo);
            }
            else
            {
                return NotFound(new { error = "Team info not available" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team info");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class SendTeamMessageRequest
{
    public string Message { get; set; } = string.Empty;
}
