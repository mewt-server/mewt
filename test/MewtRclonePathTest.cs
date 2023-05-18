/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace test;

public class MewtRclonePathTest
{
    [Fact]
    public async void PathLifeCycleTest()
    {
        var assetsPhysical = Path.Combine(Environment.CurrentDirectory, "data", "source", "assets");
        var assetsRoot = Path.GetPathRoot(assetsPhysical);
        Assert.NotNull(assetsRoot);
        var assetsRootless = assetsPhysical.Substring(assetsRoot.Length);
        var path = new MewtRclonePath(new ServerPathConfiguration()
        {
            Path = "remote:/"+ assetsRootless,
            Usage = PathUsage.Assets,
            Hash = FileHashAlgorithm.SHA1,
            Provider = FileSystemProvider.Rclone,
        });
        Assert.False((await path.IsPathWritable()).writable, "path is writable");
        var filename = Path.GetRandomFileName();
        var content = "PathLifeCycleTest.PathLifeCycleTest()";
        await Assert.ThrowsAsync<NotImplementedException>(async () => await path.WriteFile(filename, content));
        Assert.False(await path.DoesFileExist(filename));
        await Assert.ThrowsAsync<NotImplementedException>(async () => await path.DeleteFile(filename));
        await Assert.ThrowsAsync<NotImplementedException>(async () => await path.DeleteAll());
        var pathLocal = new MewtLocalFileSystemPath()
        {
            Config = new ServerPathConfiguration()
            {
                Path = Path.Combine(Directory.GetCurrentDirectory(), "cache", Path.GetRandomFileName()),
                Usage = PathUsage.Public,
                Hash = FileHashAlgorithm.SHA256,
                Provider = FileSystemProvider.Local
            }
        };
        await Assert.ThrowsAsync<NotImplementedException>(async () => await path.CopyFileFrom(filename, pathLocal));
        var files = await path.ListFiles("*");
        Assert.Contains("empty.txt", files);
        Assert.Contains("notempty.txt", files);
        Assert.True(await path.DoesFileExist("notempty.txt"), "notempty.txt");
        var expected = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "data", "source", "assets", "notempty.txt"));
        Assert.Equivalent(expected, await path.ReadFile("notempty.txt"));
        Assert.False(await path.DoesFileExist(Path.GetRandomFileName()), "should not exists");
        Assert.False(await pathLocal.DoesFileExist("notempty.txt"));
        await path.CopyFileTo("notempty.txt", pathLocal);
        Assert.True(await pathLocal.DoesFileExist("notempty.txt"));
        Assert.Equivalent(expected, await pathLocal.ReadFile("notempty.txt"));
        var metrics = await path.GetMetrics("*");
        Assert.Null(metrics.exceptions);
        Assert.Equivalent(2L, metrics.files);
        Assert.True(metrics.size > 0, "metrics.size = 0");
        var version = await path.ComputeFileVersion("notempty.txt");
        Assert.NotEmpty(version.Hash);
        Assert.Equivalent("notempty.txt", version.Name);
        Assert.True(version.Size > 0, "version.Size = 0");
        var physicalFileInfo = new FileInfo(Path.Combine(Environment.CurrentDirectory, "data", "source", "assets", "notempty.txt"));
        Assert.Equivalent(physicalFileInfo.LastWriteTime.Year, version.Modify.Year);
        Assert.True(await path.CompareFileVersion(version, false));
        Assert.True(await path.CompareFileVersion(version, true));
    }
}