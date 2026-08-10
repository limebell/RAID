using Microsoft.AspNetCore.Mvc;

namespace Raid.GameServer.Controllers;

[ApiController]
[Route("")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            service = "Raid.GameServer",
            status = "running",
            modes = new[] { "Practice", "Raid" },
            hubs = new[] { "/hubs/battle" }
        });
    }
}
