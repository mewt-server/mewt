/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Configuration;

public class ServerPathConfiguration
{
    public required string Path { get; init; }
    public required PathUsage Usage { get; init; }
    public required FileHashAlgorithm Hash { get; init; }
    public required FileSystemProvider Provider { get; init; }
    public string? UpdateCommand { get; init; }
    public IDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();
}