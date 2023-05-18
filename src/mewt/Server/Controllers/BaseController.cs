/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Microsoft.AspNetCore.Mvc;

namespace Mewt.Server.Controllers;

public abstract class BaseController<T> : ControllerBase where T : class
{
    private readonly ILogger<T> _logger;
    private readonly IMewtServer _mewt;

    public BaseController(ILogger<T> logger, IMewtServer mewt)
    {
        _logger = logger;
        _mewt = mewt;
    }

    protected ILogger<T> GetLogger() => _logger;
    protected IMewtServer GetMewt() => _mewt;
}