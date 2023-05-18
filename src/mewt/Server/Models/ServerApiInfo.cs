/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Models;

public class ServerApiInfo
{
    public ServerApiInfo(ApiType type)
    {
        Type = type;
    }
    public required bool Exists { get; init; }
    public required string PrivateFile { get; init; }
    public ApiType Type { get; init; }
}

public class ServerApiProxyInfo : ServerApiInfo
{
    public ServerApiProxyInfo() : base(ApiType.Proxy) { }
    public required string ApiProxyFile { get; init; }
}