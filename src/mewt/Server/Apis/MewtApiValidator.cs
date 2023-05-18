/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Templating;
using Scriban;
using Scriban.Runtime;

namespace Mewt.Server.Apis;

public class MewtApiValidator : IMewtApiValidator
{
    private readonly Template? _validateRequest, _configureResponse;

    public MewtApiValidator(string? validateRequest, string? configureResponse)
    {
        if (validateRequest != null)
            _validateRequest = Template.Parse(String.Concat("{{ ", validateRequest, " }}"));
        if (configureResponse != null)
            _configureResponse = Template.Parse(String.Concat("{{ ", configureResponse, " }}"));
    }

    private async ValueTask<bool> ValidateCondition(Template? condition, HttpRequest request, HttpResponse? response)
    {
        if (condition == null)
            return true;
        if (condition.HasErrors)
            return false;
        var context = new TemplateContext();
        context.PushGlobal(new ApiBuiltinFunctions());
        var data = new ScriptObject();
        data.SetValue("request", request, true);
        if (response != null)
            data.SetValue("response", response, false);
        context.PushGlobal(data);
        return await condition.EvaluateAsync(context) as bool? ?? false;
    }

    public async ValueTask<bool> ValidateRequest(HttpRequest request)
        => await ValidateCondition(_validateRequest, request, null);
    public async ValueTask<bool> ConfigureResponse(HttpRequest request, HttpResponse response)
        => await ValidateCondition(_configureResponse, request, response);

    public bool CanValidateRequest => _validateRequest != null;
    public bool CanConfigureResponse => _configureResponse != null;
}