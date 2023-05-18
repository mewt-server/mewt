/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Models;

public class FileStatusInfo
{
    public FileType Type { get; init; }
    public string Name { get; init; }
    public FileStatus Status { get; init; }

    public FileStatusInfo(FileType type, string name, FileStatus status)
    {
        Type = type;
        Name = name;
        Status = status;
    }
}