using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;

namespace Mewt.Server.Controllers;

[ApiController]
[Route("/", Order = 1)]
public class MetricsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(IWebHostEnvironment environment, ILogger<MetricsController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("healthcheck")]
    public string HealthCheck()
    {
        return "OK";
    }

    [HttpGet("metrics")]
    public string Metrics()
    {
        var metrics = new StringBuilder();
        metrics.AppendFormat("api_http_requests_total {0}", 0).AppendLine();
        return metrics.ToString();
    }
}
