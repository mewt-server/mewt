/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mewt.Server.Controllers;

[ApiController]
[Route("/", Order = 4)]
public class PageController : BaseController<PageController>
{
    public PageController(ILogger<PageController> logger, IMewtServer mewt)
        : base(logger, mewt) { }

    [HttpGet("")]
    public async ValueTask<IActionResult> Get()
        => await Get("index.html");

    [HttpGet("{*path}")]
    public async ValueTask<IActionResult> Get([FromRoute] string path)
    {
        var file = await GetMewt().CheckFile(path);
        if (file.Exists)
            return await GetFile(path, file, true);
        if (file.Type == FileType.Page)
        {
            if (await GetMewt().GeneratePage(file))
                return await GetFile(path, file, false);
            else
                return SourceFileNotFound(path, file);
        }
        else if (file.Type == FileType.Asset)
        {
            if (await GetMewt().CopyAsset(file))
                return await GetFile(path, file, false);
            else
                return SourceFileNotFound(path, file);
        }
        var lastDotIndex = path.LastIndexOf('.');
        var lastSlashIndex = path.LastIndexOf('/');
        if (lastSlashIndex > lastDotIndex || lastDotIndex == -1)
        {
            var retry = await Get(path + (lastSlashIndex == path.Length || path.Length == 0 ? null : '/') + "index.html");
            if (retry is NotFoundResult)
                return await Get(path + ".html");
            return retry;
        }
        GetLogger().LogDebug($"GET /{path} => Not found");
        return NotFound();
    }

    private async ValueTask<IActionResult> GetFile(string path, ServerFileInfo file, bool wasCached)
    {
        if (wasCached)
            GetLogger().LogDebug($"GET /{path} => Served from cache");
        else
            GetLogger().LogInformation($"GET /{path} => {file.Type} {(file.Type == FileType.Page ? "generated" : "copied")}");
        Response.Headers["X-Cache"] = wasCached ? "HIT" : "MISS";
        var fileContent = await GetMewt().GetPublicFile(file);
        if (fileContent.isPhysical && fileContent.absolutePath != null)
            return new PhysicalFileResult(fileContent.absolutePath, GetMewt().ComputeFileContentType(file));
        if (!fileContent.isPhysical && fileContent.content != null)
            return new FileContentResult(System.Text.Encoding.UTF8.GetBytes(fileContent.content), GetMewt().ComputeFileContentType(file));
        GetLogger().LogWarning($"GET /{path} => Error when sending public file");
        return StatusCode(502);
    }

    private IActionResult SourceFileNotFound(string path, ServerFileInfo file)
    {
        GetLogger().LogWarning($"GET /{path} => {file.Type} missing");
        return StatusCode(502);
    }
}
