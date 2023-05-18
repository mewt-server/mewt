/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Apis;

namespace Mewt.Server.Middlewares;

public class MewtRequestValidator
{
    private readonly IConfiguration _configuration;
    private readonly RequestDelegate _next;
    private IMewtApiValidator? _apiValidator;

    public MewtRequestValidator(IConfiguration configuration, RequestDelegate next)
    {
        _configuration = configuration;
        _next = next;
    }

    protected IMewtApiValidator ApiValidator
        => _apiValidator ??= new MewtApiValidator(
            validateRequest: _configuration.GetValue<string?>("server:http:validateRequests"),
            configureResponse: _configuration.GetValue<string?>("server:http:configureResponses")
        );

    public async Task InvokeAsync(HttpContext context)
    {
        // Validate that the request is authorized
        if (ApiValidator.CanValidateRequest && !await ApiValidator.ValidateRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        // Configure the response
        if (ApiValidator.CanConfigureResponse)
            await ApiValidator.ConfigureResponse(context.Request, context.Response);
        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}

public static class MewtRequestValidatorExtensions
{
    public static IApplicationBuilder UseMewtRequestValidator(this IApplicationBuilder builder)
        => builder.UseMiddleware<MewtRequestValidator>();
}