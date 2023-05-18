/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Text;
using System.Text.Json;
using Mewt.Server.Configuration;
using Mewt.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mewt.Server.Controllers;

[ApiController]
[Route("/api/system", Order = 1)]
[Produces("application/json")]
public class ApiSystemController : BaseController<ApiSystemController>
{
    public record DeletedFileStatus(string File, FileStatus Reason, bool Deleted);
    public record FileInfo(FileType Type, string File);

    private readonly IConfiguration _configuration;

    public ApiSystemController(IConfiguration configuration, ILogger<ApiSystemController> logger, IMewtServer mewt)
        : base(logger, mewt)
    {
        _configuration = configuration;
    }


    [HttpGet("cache/status")]
    public async ValueTask<IEnumerable<FileStatusInfo>> CheckGenerated([FromQuery] bool hash = false)
        => await GetMewt().ListGeneratedFilesStatus(hash);

    [HttpDelete("cache/all")]
    public async ValueTask<IEnumerable<string>> InvalidateAll()
        => await GetMewt().DeleteAll();

    [HttpDelete("cache/file")]
    public async ValueTask<bool> InvalidateFile([FromQuery] FileType type, [FromQuery] string file)
        => await GetMewt().DeleteFile(type, file);

    [HttpDelete("cache/files")]
    public async ValueTask<IEnumerable<FileInfo>> InvalidateFiles([FromBody] IEnumerable<FileInfo> files)
    {
        var deleted = new List<FileInfo>();
        foreach (var file in files)
            if (await GetMewt().DeleteFile(file.Type, file.File))
                deleted.Add(file);
        return deleted;
    }

    [HttpDelete("cache/outdated")]
    public async ValueTask<IEnumerable<DeletedFileStatus>> UpdateCache([FromQuery] bool hash = false)
    {
        var processed = new List<DeletedFileStatus>();
        var generated = await GetMewt().ListGeneratedFilesStatus(hash);
        foreach (var info in generated)
            if (info.Status != FileStatus.UpToDate)
                processed.Add(new DeletedFileStatus(info.Name, info.Status, await GetMewt().DeleteFile(info.Type, info.Name)));
        return processed;
    }

    [HttpGet("config")]
    [Produces("text/plain")]
    public string GetConfig()
    {
        var config = new StringBuilder();
        foreach (var entry in _configuration.AsEnumerable().ToList().OrderBy(kvp => kvp.Key))
            config.AppendLine($"{entry.Key}: {JsonSerializer.Serialize(entry.Value)}");
        return config.ToString();
    }

    [HttpPost("path/update/{usage}")]
    [Produces("text/plain")]
    public async ValueTask<string> UpdatePath(PathUsage usage)
    {
        var message = await GetMewt().UpdatePath(usage, Request, Response);
        return message;
    }
}
