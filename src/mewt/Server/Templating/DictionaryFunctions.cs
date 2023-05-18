/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Collections;
using Microsoft.Extensions.Primitives;
using Scriban.Runtime;

namespace Mewt.Server.Templating;

public class DictionaryFunctions : ScriptObject
{
    public static bool Contains(IDictionary dictionary, object key)
        => dictionary.Contains(key);

    public static bool Contains(IDictionary<string, string> dictionary, string key)
        => dictionary.ContainsKey(key);

    public static bool Contains(IDictionary<string, StringValues> dictionary, string key)
        => dictionary.ContainsKey(key);

    public static bool Contains(IEnumerable<KeyValuePair<string, string>> dictionary, string key)
        => dictionary.Any(kvp => kvp.Key == key);

    public static bool Contains(IEnumerable<KeyValuePair<string, StringValues>> dictionary, string key)
        => dictionary.Any(kvp => kvp.Key == key);

    public static object? Get(IDictionary dictionary, object key)
        => dictionary[key];

    public static string Get(IDictionary<string, string> dictionary, string key)
        => dictionary[key];

    public static string Get(IDictionary<string, StringValues> dictionary, string key)
        => dictionary[key].ToString();

    public static string Get(IEnumerable<KeyValuePair<string, string>> dictionary, string key)
        => String.Join(',', dictionary.Where(kvp => kvp.Key == key).Select(kvp => kvp.Value));

    public static string Get(IEnumerable<KeyValuePair<string, StringValues>> dictionary, string key)
    {
        var values = dictionary.Where(kvp => kvp.Key == key).Select(kvp => kvp.Value);
        if (!values.Any())
            return string.Empty;
        var concat = values.First();
        foreach (var value in values.Skip(1))
            concat = StringValues.Concat(concat, value);
        return concat.ToString();
    }

    public static void Set(IDictionary dictionary, object key, object value)
        => dictionary[key] = value;

    public static void Set(IDictionary<string, string> dictionary, string key, string value)
        => dictionary[key] = value;

    public static void Set(IDictionary<string, StringValues> dictionary, string key, string value)
        => dictionary[key] = value;
}