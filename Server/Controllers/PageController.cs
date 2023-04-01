using IO = System.IO;
using Mewt.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Scriban;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mewt.Server.Controllers;

[ApiController]
[Route("/", Order = 2)]
public class PageController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PageController> _logger;

    public PageController(IWebHostEnvironment environment, ILogger<PageController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("{*path}")]
    public IActionResult Get([FromRoute] string path)
    {
        var fullPath = $"{_environment.ContentRootPath}/public/{path}";
        if (IO.File.Exists(fullPath))
        {
            return new PhysicalFileResult(fullPath, "text/html");
        }
        var descriptorPath = $"{_environment.ContentRootPath}/source/pages/{path}.yml";
        if (IO.File.Exists(descriptorPath))
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();
            var pd = deserializer.Deserialize<PageDescriptor>(IO.File.ReadAllTextAsync(descriptorPath).Result);
            var templatePath = $"{_environment.ContentRootPath}/source/templates/{pd.TemplateName}";
            if (IO.File.Exists(templatePath))
            {
                var template = Template.Parse(IO.File.ReadAllTextAsync(templatePath).Result);
                var content = template.RenderAsync(pd.Content).Result;
                IO.File.WriteAllTextAsync(fullPath, content);
                Response.ContentType = pd.ContentType;
                return Ok(content);
            }
        }
        return NotFound();
    }
}
