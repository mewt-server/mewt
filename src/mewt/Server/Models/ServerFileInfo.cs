/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace Mewt.Server.Models;

public class ServerFileInfo
{
    public ServerFileInfo(FileType type)
    {
        Type = type;
    }

    public required bool Exists { get; init; }
    public required string PublicFile { get; init; }
    public FileType Type { get; init; }
}

public class ServerAssetInfo : ServerFileInfo
{
    public ServerAssetInfo() : base(FileType.Asset) { }
    public required string Asset { get; init; }
}

public class ServerPageInfo : ServerFileInfo
{
    public ServerPageInfo() : base(FileType.Page) { }
    public required string PageDescriptor { get; init; }
}