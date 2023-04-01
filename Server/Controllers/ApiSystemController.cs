using Microsoft.AspNetCore.Mvc;

namespace Mewt.Server.Controllers;

[ApiController]
[Route("/api/system")]
public class ApiSystemController : ControllerBase
{
    private readonly ILogger<ApiSystemController> _logger;

    public ApiSystemController(ILogger<ApiSystemController> logger)
    {
        _logger = logger;
    }
    
    [HttpPost("generate")]
    public IActionResult Generate()
    {
        return NoContent();
    }

    [HttpPost("invalidate")]
    public IActionResult Invalidate()
    {
        return NoContent();
    }

}
