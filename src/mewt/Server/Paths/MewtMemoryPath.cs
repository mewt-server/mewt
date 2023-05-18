/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Mewt.Server.Configuration;
using Mewt.Server.Models;
using System.Runtime.Caching;

namespace Mewt.Server.Paths;

public class MewtMemoryPath : IMewtPath
{
    private readonly MemoryCache _files;
    public ServerPathConfiguration Config { get; init; }

    public MewtMemoryPath(ServerPathConfiguration config)
    {
        Config = config;
        _files = new MemoryCache($"MewtMemoryPath_{Config.Usage}");
    }

    public ValueTask<bool> CompareFileVersion(FileVersion version, bool hash)
    {
        var expectedVersion = (_files[version.Name] as (FileVersion version, string content)?)?.version;
        if (expectedVersion == null)
            return ValueTask.FromResult<bool>(false);
        if (expectedVersion.Size != version.Size)
            return ValueTask.FromResult<bool>(false);
        if (expectedVersion.Modify != version.Modify)
            return ValueTask.FromResult<bool>(false);
        if (!hash)
            return ValueTask.FromResult<bool>(true);
        return ValueTask.FromResult<bool>(expectedVersion.Hash == version.Hash);
    }

    public ValueTask<FileVersion> ComputeFileVersion(string relativePath)
    {
        var version = (_files[relativePath] as (FileVersion version, string content)?)?.version;
        if (version == null)
            throw new FileNotFoundException(relativePath);
        return ValueTask.FromResult<FileVersion>(version);
    }

    public ValueTask CopyFileFrom(string relativePath, IMewtPath source)
        => CopyFileFrom(relativePath, source, relativePath);

    public async ValueTask CopyFileFrom(string relativePath, IMewtPath source, string target)
    {
        if (Config.Usage == PathUsage.Private && source.Config.Usage == PathUsage.Apis)
            await WriteFile(target, await source.ReadFile(relativePath));
    }

    public ValueTask<IEnumerable<(string file, Exception exception)>> DeleteAll()
    {
        var keys = _files.Select(kvp => kvp.Key);
        foreach(var key in keys)
            _files.Remove(key);
        return ValueTask.FromResult<IEnumerable<(string file, Exception exception)>>(new List<(string file, Exception exception)>());
    }

    public ValueTask DeleteFile(string relativePath)
    {
        _files.Remove(relativePath);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> DoesFileExist(string relativePath)
        => ValueTask.FromResult<bool>(_files.Contains(relativePath));

    public ValueTask<(long files, long size, IEnumerable<Exception>? exceptions)> GetMetrics(string searchPattern)
        => ValueTask.FromResult<(long, long, IEnumerable<Exception>?)>((
            _files.GetCount(),
            _files.Sum(kv => (kv.Value as (FileVersion version, string content)?)?.version.Size) ?? 0,
            null
        ));

    public ValueTask<(bool writable, Exception? exception)> IsPathWritable()
        => ValueTask.FromResult<(bool, Exception?)>((true, null));

    public ValueTask<IEnumerable<string>> ListFiles(string pattern)
        => ValueTask.FromResult<IEnumerable<string>>(_files.Select(kv => kv.Key));

    public ValueTask<string> ReadFile(string relativePath)
    {
        var content = (_files[relativePath] as (FileVersion version, string content)?)?.content;
        if (content == null)
            throw new FileNotFoundException(relativePath);
        return ValueTask.FromResult<string>(content);
    }

    public ValueTask<bool> Update()
        => ValueTask.FromResult<bool>(false);

    public ValueTask WriteFile(string relativePath, string content)
    {
        _files.Add(relativePath, (new FileVersion()
        {
            Name = relativePath,
            Modify = DateTime.Now,
            Size = content.Length,
            Hash = content.GetHashCode().ToString("X8"),
        }, content), new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, 5, 0) });
        return ValueTask.CompletedTask;
    }
}