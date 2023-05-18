/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Models;
using Mewt.Server.Templating;
using Scriban;
using Scriban.Runtime;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mewt.Server.Apis;

public class MewtApiProxy : IMewtApi
{
    public readonly string _pwd = Environment.CurrentDirectory;
    public required Template? ValidateRequest { get; init; }
    public required Template? ConfigureCall { get; init; }
    public required Template? MakePayload { get; init; }
    public required Template ConfigureResponse { get; init; }
    public required Template? MakeResponse { get; init; }

    public static MewtApiProxy? Build(string descriptorContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var apd = deserializer.Deserialize<ApiProxyDescriptor>(descriptorContent);
        if (apd == null)
            return null;
        return new MewtApiProxy()
        {
            ValidateRequest = apd.ValidateRequest == null ? null : Template.Parse(String.Concat("{{ ", apd.ValidateRequest, " }}")),
            ConfigureCall = apd.ConfigureCall == null ? null : Template.Parse(String.Concat("{{ ", apd.ConfigureCall, " }}")),
            MakePayload = apd.MakePayload == null ? null : Template.Parse(apd.MakePayload),
            ConfigureResponse = Template.Parse(String.Concat("{{ ", apd.ConfigureResponse, " }}")),
            MakeResponse = apd.MakeResponse == null ? null : Template.Parse(apd.MakeResponse)
        };
    }

    public async ValueTask<(ApiStatus status, object? body, Exception? exception)> Execute(HttpRequest request, HttpResponse response)
    {
        try
        {
            if (ValidateRequest != null && !await CanExecute(request))
                return (ApiStatus.Forbidden, null, null);
            var result = await CallRemoteApi(request);
            var body = await ConfigureClientResponse(request, result, response);
            return (ApiStatus.Success, body, null);
        }
        catch (Exception e) { return (ApiStatus.Error, null, e); }
    }

    private async ValueTask<bool> CanExecute(HttpRequest request)
    {
        if (ValidateRequest == null)
            return true;
        var context = new TemplateContext();
        context.PushGlobal(new ApiBuiltinFunctions());
        var data = new ScriptObject();
        data.SetValue("pwd", _pwd, true);
        data.SetValue("request", request, true);
        context.PushGlobal(data);
        var executable = await ValidateRequest.EvaluateAsync(context);
        return executable as bool? ?? false;
    }

    private async ValueTask<HttpResponseMessage?> CallRemoteApi(HttpRequest request)
    {
        if (ConfigureCall == null)
            return null;
        using var handler = new HttpClientHandler();
        using var client = new HttpClient(handler);
        using var message = new HttpRequestMessage();
        var context = new TemplateContext();
        context.PushGlobal(new ApiBuiltinFunctions());
        var data = new ScriptObject();
        data.SetValue("pwd", _pwd, true);
        data.SetValue("request", request, true);
        data.SetValue("client", client, true);
        data.SetValue("message", message, true);
        context.PushGlobal(data);
        var content = await ConfigureCall.EvaluateAsync(context);
        if (MakePayload != null)
            content = await MakePayload.RenderAsync(context);
        if (content != null)
        {
            if (content is string)
                message.Content = new StringContent(content as string ?? string.Empty);
            else if (content is object)
                message.Content = JsonContent.Create(content);
        }
        return await client.SendAsync(message);
    }

    private async ValueTask<object> ConfigureClientResponse(HttpRequest request, HttpResponseMessage? result, HttpResponse response)
    {
        var context = new TemplateContext();
        context.PushGlobal(new ApiBuiltinFunctions());
        var data = new ScriptObject();
        data.SetValue("pwd", _pwd, true);
        data.SetValue("request", request, true);
        if (result != null)
            data.SetValue("result", result, true);
        data.SetValue("response", response, true);
        context.PushGlobal(data);
        var body = await ConfigureResponse.EvaluateAsync(context);
        if (MakeResponse != null)
            body = await MakeResponse.RenderAsync(context);
        return body;
    }
}