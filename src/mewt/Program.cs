/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server;
using Mewt.Server.Middlewares;
using Microsoft.AspNetCore.Rewrite;
using System.Text.Json.Serialization;

Console.WriteLine($"Mewt (v{(typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "?")}), copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>.");
Console.WriteLine("Mewt is distributed under the terms of the AGPL-3.0-only license, see <https://github.com/mewt-server/mewt> for details.");

var builder = WebApplication.CreateBuilder(args);

// Add configuration sources to the container.
builder.Configuration.Sources.Clear();
builder.Configuration.AddEnvironmentVariables(prefix: "ASPNETCORE_");
builder.Configuration.AddEnvironmentVariables(prefix: "DOTNET_");
builder.Configuration.AddYamlFile(path: Path.Combine(Environment.CurrentDirectory, "mewt.yml"), optional: true);
builder.Configuration.AddEnvironmentVariables(prefix: "MEWT_");
builder.Configuration.AddCommandLine(args);

// Configure logs on Console
builder.Logging.ClearProviders().AddSimpleConsole(c =>
{
    c.SingleLine = true;
    c.TimestampFormat = builder.Configuration.GetValue<string>("logging:timestampFormat", "[yyyy-MM-dd HH:mm:ss.fff] ");
});

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IMewtServer, MewtServer>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var mewt = app.Services.GetService<IMewtServer>();
if (mewt != null)
{
    var healthy = true;
    foreach (var check in await mewt.HealthCheck())
    {
        if (!check.Value.Success)
        {
            healthy = false;
            Console.WriteLine($"Mewt healthcheck test `{check.Key}` has failed with reason: '{check.Value.Error}'.");
        }
    }
    if (!healthy)
        return 1;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("server:swagger:enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var rewriteOptions = new RewriteOptions();
foreach (var redirect in app.Configuration.GetSection("server:http:redirects").GetChildren())
    rewriteOptions.AddRedirect(
        redirect.GetValue<string>("regex") ?? string.Empty,
        redirect.GetValue<string>("replacement") ?? string.Empty,
        redirect.GetValue<int>("statusCode", StatusCodes.Status302Found));
foreach (var rewrite in app.Configuration.GetSection("server:http:rewrites").GetChildren())
    rewriteOptions.AddRewrite(
        rewrite.GetValue<string>("regex") ?? string.Empty,
        rewrite.GetValue<string>("replacement") ?? string.Empty,
        rewrite.GetValue<bool>("skipRemainingRules", false));
if (rewriteOptions.Rules.Any())
    app.UseRewriter(rewriteOptions);

app.UseMewtResponseHeaders();

if (!String.IsNullOrEmpty(app.Configuration.GetValue<string?>("server:http:validateRequests"))
    && String.IsNullOrEmpty(app.Configuration.GetValue<string?>("server:http:configureResponses")))
    app.UseMewtRequestValidator();

app.MapControllers();

app.Run();

return 0;

public partial class Program { } // For xUnit-testing purpose