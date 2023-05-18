/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Apis;

public interface IMewtApiValidator
{
    bool CanValidateRequest { get; }
    bool CanConfigureResponse { get; }
    ValueTask<bool> ValidateRequest(HttpRequest request);
    ValueTask<bool> ConfigureResponse(HttpRequest request, HttpResponse response);
}