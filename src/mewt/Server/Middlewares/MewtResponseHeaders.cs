/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Reflection;

namespace Mewt.Server.Middlewares;

public class MewtResponseHeaders
{
    private readonly RequestDelegate _next;
    private readonly string _server;
    private readonly string _xServedBy;

    public MewtResponseHeaders(RequestDelegate next)
    {
        _next = next;
        _server = $"Mewt/{(GetType().Assembly.GetName().Version?.ToString(3) ?? "?")} (https://github.com/mewt-server/mewt)";
        _xServedBy = Environment.MachineName.ToLowerInvariant();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add headers
        context.Response.Headers["Server"] = _server;
        context.Response.Headers["X-Served-By"] = _xServedBy;
        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}

public static class MewtResponseHeadersExtensions
{
    public static IApplicationBuilder UseMewtResponseHeaders(this IApplicationBuilder builder)
        => builder.UseMiddleware<MewtResponseHeaders>();
}