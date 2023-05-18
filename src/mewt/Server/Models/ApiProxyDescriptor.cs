/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Models;

public class ApiProxyDescriptor
{
    public string? ValidateRequest { get; init; }
    public required string ConfigureCall { get; init; }
    public string? MakePayload { get; init; }
    public required string ConfigureResponse { get; init; }
    public string? MakeResponse { get; init; }
}