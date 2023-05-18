/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Apis;
using Mewt.Server.Configuration;
using Mewt.Server.Models;

namespace Mewt.Server;

public interface IMewtServer
{
    ValueTask<ServerApiInfo> CheckApi(ApiType type, string relativePath);
    ValueTask<ServerFileInfo> CheckFile(string relativePath);
    ValueTask<bool> CopyApi(ServerApiInfo api);
    ValueTask<bool> CopyAsset(ServerFileInfo asset);
    ValueTask<bool> GeneratePage(ServerFileInfo page);
    string ComputeFileContentType(ServerFileInfo file);
    ValueTask<(ApiStatus status, object? body)> ExecuteApi(ServerApiInfo apiInfo, HttpRequest request, HttpResponse response);
    ValueTask<(bool isPhysical, string? absolutePath, string? content)> GetPublicFile(ServerFileInfo file);
    ValueTask<IEnumerable<FileStatusInfo>> ListGeneratedFilesStatus(bool hash);
    ValueTask<bool> DeleteFile(FileType type, string file);
    ValueTask<IEnumerable<string>> DeleteAll();
    ValueTask<IDictionary<string, HealthCheck>> HealthCheck();
    ValueTask<IDictionary<string, object>> GetMetrics();
    ValueTask<string> UpdatePath(PathUsage path, HttpRequest request, HttpResponse response);
}