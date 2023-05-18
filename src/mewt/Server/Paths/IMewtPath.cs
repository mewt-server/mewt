/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Configuration;
using Mewt.Server.Models;

namespace Mewt.Server.Paths;

public interface IMewtPath
{
    ServerPathConfiguration Config { get; }
    ValueTask<bool> DoesFileExist(string relativePath);
    ValueTask<string> ReadFile(string relativePath);
    ValueTask CopyFileFrom(string relativePath, IMewtPath source);
    ValueTask CopyFileFrom(string relativePath, IMewtPath source, string target);
    ValueTask WriteFile(string relativePath, string content);
    ValueTask<bool> CompareFileVersion(FileVersion version, bool hash);
    ValueTask<FileVersion> ComputeFileVersion(string relativePath);
    ValueTask<IEnumerable<string>> ListFiles(string pattern);
    ValueTask DeleteFile(string relativePath);
    ValueTask<IEnumerable<(string file, Exception exception)>> DeleteAll();
    ValueTask<(bool writable, Exception? exception)> IsPathWritable();
    ValueTask<(long files, long size, IEnumerable<Exception>? exceptions)> GetMetrics(string searchPattern);
}