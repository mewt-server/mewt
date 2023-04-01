namespace Mewt.Server.Models;

public class Status
{
    public int Code { get; set; } = 0;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
}