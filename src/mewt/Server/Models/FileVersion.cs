/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Models;

public class FileVersion
{
    public required string Name { get; init; }
    public required string Hash { get; init; }
    public required long Size { get; init; }
    public required DateTime Modify { get; init; }
}