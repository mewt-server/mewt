/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Text.Json;
using Scriban.Runtime;

namespace Mewt.Server.Templating;

public class JsonFunctions : ScriptObject
{
    public static object? From(string json)
        => JsonSerializer.Deserialize<object>(json); 

    public static string To(object graph)
        => JsonSerializer.Serialize(graph);
}