/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Models;

public class FileMetadata
{
    public required FileType Type { get; init; }
    public FileVersion? Private { get; init; }
    public FileVersion? Public { get; init; }
    public FileVersion? Api { get; set; }
    public FileVersion? Asset { get; set; }
    public FileVersion? Page { get; set; }
    public List<FileVersion>? Contents { get; set; }
    public List<FileVersion>? Templates { get; set; }
}