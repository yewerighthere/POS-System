using Microsoft.AspNetCore.Mvc;

namespace InventoryManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    [HttpPost("deduct")]
    public IActionResult Deduct()
    {
        return Ok();
    }

    [HttpPost("restock")]
    public IActionResult Restock()
    {
        return Ok();
    }
}
