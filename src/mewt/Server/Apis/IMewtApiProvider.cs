/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Models;

namespace Mewt.Server.Apis;

public interface IMewtApiProvider
{
    ValueTask<IMewtApi?> GetApi(ServerApiInfo apiInfo);
    void RemoveApi(string privateRelativePath);
    void Clear();
}