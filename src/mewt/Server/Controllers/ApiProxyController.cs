/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mewt.Server.Controllers;

[ApiController]
[Route("/api/proxy", Order = 3)]
public class ApiProxyController : BaseController<ApiProxyController>
{
    public ApiProxyController(ILogger<ApiProxyController> logger, IMewtServer mewt)
        : base(logger, mewt) { }

    [HttpDelete("{path}")]
    [HttpGet("{path}")]
    [HttpHead("{path}")]
    [HttpOptions("{path}")]
    [HttpPatch("{path}")]
    [HttpPost("{path}")]
    [HttpPut("{path}")]
    public async ValueTask<object> Call([FromRoute] string path)
    {

        var api = await GetMewt().CheckApi(ApiType.Proxy, path);
        if (api.Exists)
        {
            return await Execute(path, api, true);
        }
        if (api.Type == ApiType.Proxy)
        {
            if (await GetMewt().CopyApi(api))
            {
                return await Execute(path, api, false);
            }
            else
            {
                GetLogger().LogWarning($"{Request.Method} /{path} => {api.Type} missing");
                return StatusCode(502);
            }
        }
        GetLogger().LogDebug($"{Request.Method} /{path} => Not found");
        return NotFound();
    }

    private async ValueTask<object> Execute(string path, ServerApiInfo api, bool wasCached)
    {
        var result = await GetMewt().ExecuteApi(api, Request, Response);
        if (result.status == ApiStatus.Success)
            return result.body ?? string.Empty;
        if (result.status == ApiStatus.Forbidden)
            return StatusCode(StatusCodes.Status405MethodNotAllowed);
        if (result.status == ApiStatus.Error)
            return StatusCode(StatusCodes.Status500InternalServerError);
        return Empty;
    }
}
