/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Security.Cryptography;
using Mewt.Server.Configuration;
using Mewt.Server.Models;

namespace Mewt.Server.Paths;

public class MewtLocalFileSystemPath : IMewtPath
{
    public required ServerPathConfiguration Config { get; init; }

    public ValueTask<bool> DoesFileExist(string relativePath)
        => ValueTask.FromResult<bool>(File.Exists(Path.Combine(Config.Path, relativePath)));

    public async ValueTask<string> ReadFile(string relativePath)
        => await File.ReadAllTextAsync(Path.Combine(Config.Path, relativePath));

    public async ValueTask CopyFileFrom(string relativePath, IMewtPath source)
        => await CopyFileFrom(relativePath, source, relativePath);

    public async ValueTask CopyFileFrom(string relativePath, IMewtPath source, string target)
    {
        await MakeDirectoryAsync(target);
        await Task.Run(() => File.Copy(Path.Combine(source.Config.Path, relativePath), Path.Combine(Config.Path, target), true));
    }

    public async ValueTask WriteFile(string relativePath, string content)
    {
        await MakeDirectoryAsync(relativePath);
        await File.WriteAllTextAsync(Path.Combine(Config.Path, relativePath), content);
    }

    private HashAlgorithm CreateHash()
    {
        switch (Config.Hash)
        {
            case FileHashAlgorithm.MD5: return MD5.Create();
            case FileHashAlgorithm.SHA1: return SHA1.Create();
            case FileHashAlgorithm.SHA256: return SHA256.Create();
            case FileHashAlgorithm.SHA384: return SHA384.Create();
            case FileHashAlgorithm.SHA512: return SHA512.Create();
        }
        throw new InvalidOperationException($"HashAlgorithm `{Config.Hash}` is not supported.");
    }

    private async ValueTask<string> ComputeFileHash(string relativePath)
    {
        using (var hash = CreateHash())
        using (FileStream fileStream = File.OpenRead(Path.Combine(Config.Path, relativePath)))
            return BitConverter.ToString(await hash.ComputeHashAsync(fileStream)).Replace("-", "").ToLowerInvariant();
    }

    public async ValueTask<bool> CompareFileVersion(FileVersion version, bool hash)
    {
        var fileInfo = new FileInfo(Path.Combine(Config.Path, version.Name));
        if (version.Size != fileInfo.Length)
            return false;
        if (version.Modify != fileInfo.LastWriteTime)
            return false;
        if (!hash)
            return true;
        return version.Hash == await ComputeFileHash(version.Name);
    }

    public async ValueTask<FileVersion> ComputeFileVersion(string relativePath)
    {
        var fileInfo = new FileInfo(Path.Combine(Config.Path, relativePath));
        return new FileVersion()
        {
            Name = relativePath,
            Hash = await ComputeFileHash(relativePath),
            Size = fileInfo.Length,
            Modify = fileInfo.LastWriteTime
        };
    }

    private async ValueTask MakeDirectoryAsync(string relativePath)
    {
        var parent = Directory.GetParent(Path.Combine(Config.Path, relativePath));
        if (parent != null)
            await Task.Run(() => Directory.CreateDirectory(parent.FullName));
    }

    public async ValueTask<IEnumerable<string>> ListFiles(string pattern)
        => (await Task.Run(() => Directory.EnumerateFiles(Config.Path, pattern, SearchOption.AllDirectories))).Select(file => file.Remove(0, Config.Path.Length + 1));

    public ValueTask DeleteFile(string relativePath)
    {
        File.Delete(Path.Combine(Config.Path, relativePath));
        return ValueTask.CompletedTask;
    }

    public async ValueTask<IEnumerable<(string file, Exception exception)>> DeleteAll()
    {
        var errors = new List<(string file, Exception exception)>();
        var di = new DirectoryInfo(Config.Path);
        await Task.Run(() =>
        {
            foreach (var file in di.GetFiles())
            {
                try { file.Delete(); }
                catch (Exception e) { errors.Add((file: file.Name, exception: e)); }
            }
            foreach (var dir in di.GetDirectories())
            {
                try { dir.Delete(true); }
                catch (Exception e) { errors.Add((file: dir.Name, exception: e)); }
            }
        });
        return errors;
    }

    public async ValueTask<(bool writable, Exception? exception)> IsPathWritable()
    {
        try
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(Config.Path))
                    Directory.CreateDirectory(Config.Path);
                using (var file = File.Create(Path.Combine(Config.Path, Path.GetRandomFileName()) + ".tmp", 1, FileOptions.DeleteOnClose))
                {
                    file.Close();
                }
            });
            return (writable: true, exception: null);
        }
        catch (Exception e)
        {
            return (writable: false, exception: e);
        }
    }

    public async ValueTask<(long files, long size, IEnumerable<Exception>? exceptions)> GetMetrics(string searchPattern)
    {
        long files = 0, size = 0;
        var exceptions = new List<Exception>();
        await Task.Run(() =>
        {
            try
            {
                var di = new DirectoryInfo(Config.Path);
                var fis = di.GetFiles(searchPattern, SearchOption.AllDirectories);
                files = fis.LongLength;
                foreach (var fi in fis)
                {
                    try { size += fi.Length; }
                    catch (Exception e) { exceptions.Add(e); }
                }
            }
            catch (Exception e) { exceptions.Add(e); }
        });
        return (files, size, exceptions.Any() ? exceptions : null);
    }
}