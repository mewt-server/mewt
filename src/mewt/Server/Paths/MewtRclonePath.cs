/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Diagnostics;
using System.Text.RegularExpressions;
using Mewt.Server.Configuration;
using Mewt.Server.Models;

namespace Mewt.Server.Paths;

public class MewtRclonePath : IMewtPath
{
    private readonly string _rcloneConfig;
    private readonly string _rcloneLocal;
    private readonly string _rclonePath;
    private readonly Regex _regexHashMd5 = new Regex(@"""MD5""\s*:\s*""([^""]+)""");
    private readonly Regex _regexHashSha1 = new Regex(@"""SHA-1""\s*:\s*""([^""]+)""");
    private readonly Regex _regexModify = new Regex(@"""ModTime""\s*:\s*""([^""]+)""");
    private readonly Regex _regexSize = new Regex(@"""Size""\s*:\s*(\d+)");
    public ServerPathConfiguration Config { get; init; }

    public MewtRclonePath(ServerPathConfiguration config)
    {
        if (!(config.Hash == FileHashAlgorithm.MD5 || config.Hash == FileHashAlgorithm.SHA1))
            throw new ArgumentException($"Hash must be one of: `MD5`, `SHA1`; but `{config.Hash}` given.", "config.Hash");
        if (config.Usage == PathUsage.Public || config.Usage == PathUsage.Metadata)
            throw new ArgumentException($"Usage must be one of: `Assets`, `Contents`, `Pages`, `Templates`; but `{config.Usage}` given.", "config.Usage");
        Config = config;
        _rcloneConfig = Config.Options.ContainsKey("rcloneconfig") && Config.Options["rcloneconfig"] is not null ? Config.Options["rcloneconfig"] + " " : string.Empty;
        _rcloneLocal = Config.Options.ContainsKey("rclonelocal") && Config.Options["rclonelocal"] is not null ? Config.Options["rclonelocal"] : "local:";
        _rclonePath = Config.Options.ContainsKey("rclonepath") && Config.Options["rclonepath"] is not null ? Config.Options["rclonepath"] : "rclone";
    }

    public async ValueTask<(int exitCode, string output, string error)> ExecRcloneCommand(string command)
    {
        var startInfo = new ProcessStartInfo(_rclonePath, _rcloneConfig + command)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory
        };
        using (var process = Process.Start(startInfo))
        {
            if (process == null)
                throw new InvalidOperationException($"Cannot run rclone command: `{startInfo.FileName} {startInfo.Arguments}`.");
            await process.WaitForExitAsync();
            return (
                exitCode: process.ExitCode,
                output: await process.StandardOutput.ReadToEndAsync(),
                error: await process.StandardError.ReadToEndAsync()
            );
        }
    }

    public async ValueTask<bool> CompareFileVersion(FileVersion version, bool hash)
    {
        var exec = await ExecRcloneCommand(String.Concat("lsjson --files-only ",
            hash ? "--hash --hash-type " + (Config.Hash == FileHashAlgorithm.MD5 ? "MD5" : Config.Hash == FileHashAlgorithm.SHA1 ? "SHA-1" : "None") : string.Empty,
            " --no-mimetype ", Config.Path, "/", version.Name));
        if (hash && Config.Hash == FileHashAlgorithm.MD5 && version.Hash == _regexHashMd5.Match(exec.output).Groups[1].Value)
            return true;
        if (hash && Config.Hash == FileHashAlgorithm.SHA1 && version.Hash == _regexHashSha1.Match(exec.output).Groups[1].Value)
            return true;
        if (version.Size != long.Parse(_regexSize.Match(exec.output).Groups[1].Value))
            return false;
        return version.Modify == DateTime.Parse(_regexModify.Match(exec.output).Groups[1].Value);
    }

    public async ValueTask<FileVersion> ComputeFileVersion(string relativePath)
    {
        var exec = await ExecRcloneCommand(String.Concat("lsjson --files-only --hash --hash-type ",
            Config.Hash == FileHashAlgorithm.MD5 ? "MD5" : Config.Hash == FileHashAlgorithm.SHA1 ? "SHA-1" : "None",
            " --no-mimetype ", Config.Path, "/", relativePath));
        return new FileVersion()
        {
            Hash = Config.Hash == FileHashAlgorithm.MD5 ? _regexHashMd5.Match(exec.output).Groups[1].Value
                : Config.Hash == FileHashAlgorithm.SHA1 ? _regexHashSha1.Match(exec.output).Groups[1].Value : string.Empty,
            Modify = DateTime.Parse(_regexModify.Match(exec.output).Groups[1].Value),
            Name = relativePath,
            Size = long.Parse(_regexSize.Match(exec.output).Groups[1].Value)
        };
    }

    public ValueTask CopyFileFrom(string relativePath, IMewtPath source)
        => CopyFileFrom(relativePath, source, string.Empty);

    public ValueTask CopyFileFrom(string relativePath, IMewtPath source, string target)
        => throw new NotImplementedException("Rclone mounts are read-only for Mewt.");

    public async ValueTask CopyFileTo(string relativePath, IMewtPath target)
    {
        if (target.Config.Provider == FileSystemProvider.Local)
        {
            var exec = await ExecRcloneCommand(String.Concat("copyto ", Config.Path, "/", relativePath, " ", _rcloneLocal, "/", target.Config.Path, "/", relativePath));
            if (exec.exitCode != 0)
                throw new IOException(exec.error);
        }
    }

    public ValueTask<IEnumerable<(string file, Exception exception)>> DeleteAll()
        => throw new NotImplementedException("Rclone mounts are read-only for Mewt.");

    public ValueTask DeleteFile(string relativePath)
        => throw new NotImplementedException("Rclone mounts are read-only for Mewt.");

    public async ValueTask<bool> DoesFileExist(string relativePath)
    {
        var exec = await ExecRcloneCommand(String.Concat("lsjson --files-only --no-modtime --no-mimetype ", Config.Path, "/", relativePath));
        return exec.exitCode == 0;
    }

    public async ValueTask<(long files, long size, IEnumerable<Exception>? exceptions)> GetMetrics(string searchPattern)
    {
        var ls = await ExecRcloneCommand(String.Concat("lsf --recursive --files-only --include \"", searchPattern, "\" --format sp ", Config.Path));
        var lines = ls.output.Split(
            new string[] { "\r\n", "\r", "\n" },
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
        long size = 0;
        foreach (var line in lines)
        {
            var split = line.Split(';', StringSplitOptions.None);
            if (long.TryParse(split[0], out var fileSize))
                size += fileSize;
        }
        return (lines.LongLength, size, null);
    }

    public ValueTask<(bool writable, Exception? exception)> IsPathWritable()
        => ValueTask.FromResult<(bool, Exception?)>((false, new NotImplementedException("Rclone mounts are read-only for Mewt.")));

    public async ValueTask<IEnumerable<string>> ListFiles(string pattern)
    {
        var exec = await ExecRcloneCommand(String.Concat("lsf --recursive --files-only --include \"", pattern, "\" ", Config.Path));
        return exec.output.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public async ValueTask<string> ReadFile(string relativePath)
    {
        var exec = await ExecRcloneCommand(String.Concat("cat ", Config.Path, "/", relativePath));
        return exec.output;
    }

    public ValueTask WriteFile(string relativePath, string content)
        => throw new NotImplementedException("Rclone mounts are read-only for Mewt.");
}