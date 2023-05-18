/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Text;
using Scriban.Runtime;

namespace Mewt.Server.Templating;

public class StreamFunctions : ScriptObject
{
    public static async ValueTask<string> Read(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();
        stream.Position = 0;
        return text;
    }
}