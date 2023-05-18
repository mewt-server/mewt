/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Scriban.Runtime;

namespace Mewt.Server.Templating;

/// <summary>
/// This class is highly inspired from the BuiltinFunctions class of Scriban copyrighted by Alexandre Mutel under BSD-Clause 2 license:
/// https://github.com/scriban/scriban/blob/3912f57327e1e14cac521fdeb9b7b00d74b0aee3/src/Scriban/Functions/BuiltinFunctions.cs
/// </summary>
public class ApiBuiltinFunctions : ScriptObject
{
    private static readonly ScriptObject Default = new DefaultBuiltins();

    public ApiBuiltinFunctions() : base(2)
    {
        ((ScriptObject)Default.Clone(true)).CopyTo(this);
    }

    private class DefaultBuiltins : ScriptObject
    {
        public DefaultBuiltins() : base(2, false)
        {
            SetValue("cmd", new CommandFunctions(), true);
            SetValue("dict", new DictionaryFunctions(), true);
            SetValue("http", new HttpFunctions(), true);
            SetValue("json", new JsonFunctions(), true);
            SetValue("stream", new StreamFunctions(), true);
            SetValue("yaml", new YamlFunctions(), true);
        }
    }
}