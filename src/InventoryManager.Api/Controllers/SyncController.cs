using Microsoft.AspNetCore.Mvc;

namespace InventoryManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    [HttpGet("catalog")]
    public IActionResult GetCatalog()
    {
        return Ok();
    }

    [HttpGet("stock")]
    public IActionResult GetStock()
    {
        return Ok();
    }
}
