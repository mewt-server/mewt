/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Models;

public class HealthCheck
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public HealthCheck(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }
}