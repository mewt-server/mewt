/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Paths;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Mewt.Server.Templating;

public class TemplateLoader : ITemplateLoader
{
    public required IEnumerable<string>? AvailableTemplates { get; init; }
    public required IMewtPath TemplatesPath { get; init; }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        if (AvailableTemplates == null)
            return string.Empty;
        if (AvailableTemplates.Contains(templateName))
            return templateName;
        return string.Empty;
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        => LoadAsync(context, callerSpan, templatePath).Result;

    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
            return string.Empty;
        if (!await TemplatesPath.DoesFileExist(templatePath))
            return string.Empty;
        return await TemplatesPath.ReadFile(templatePath);
    }
}