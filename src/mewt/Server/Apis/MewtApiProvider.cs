/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Runtime.Caching;
using Mewt.Server.Models;
using Mewt.Server.Paths;

namespace Mewt.Server.Apis;

public class MewtApiProvider : IMewtApiProvider
{
    private readonly MemoryCache _apis;
    private readonly IMewtPath _privatePath;

    public MewtApiProvider(IMewtPath privatePath)
    {
        _privatePath = privatePath;
        _apis = new MemoryCache($"MewtApiProvider");
    }

    public async ValueTask<IMewtApi?> GetApi(ServerApiInfo apiInfo)
    {
        IMewtApi? api = _apis[apiInfo.PrivateFile] as IMewtApi;
        if (api != null)
            return api;
        if (apiInfo.Type == ApiType.Proxy)
        {
            if (!await _privatePath.DoesFileExist(apiInfo.PrivateFile))
                return null;
            api = MewtApiProxy.Build(await _privatePath.ReadFile(apiInfo.PrivateFile));
            if (api == null)
                return null;
            _apis.Add(apiInfo.PrivateFile, api, new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, 5, 0) });
            return api;
        }
        return null;
    }

    public void RemoveApi(string privateRelativePath)
    {
        _apis.Remove(privateRelativePath);
    }

    public void Clear()
    {
        _apis.Trim(100);
    }
}