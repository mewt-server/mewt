using System.Dynamic;

namespace Mewt.Server.Models;

public class PageDescriptor
{
    public string TemplateName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, object> Content { get; set; } = new Dictionary<string, object>();
}