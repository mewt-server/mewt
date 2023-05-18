/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using YamlDotNet.Serialization;

namespace Mewt.Server.Models;

public class PageDescriptor
{
    [YamlMember(Alias = "content")]
    public IDictionary<string, object>? Content { get; init; }
    [YamlMember(Alias = "contentFiles")]
    public IEnumerable<string>? ContentFiles { get; init; }
    [YamlMember(Alias = "templateIncludes")]
    public IEnumerable<string>? TemplateIncludes { get; init; }
    [YamlMember(Alias = "templateFiles")]
    public required IEnumerable<string> TemplateFiles { get; init; }
}