/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

namespace test;

public class MewtLocalFileSystemPathTest
{
    [Fact]
    public async void PathLifeCycleTest()
    {
        var path = new MewtLocalFileSystemPath()
        {
            Config = new ServerPathConfiguration()
            {
                Path = Path.Combine(Directory.GetCurrentDirectory(), "cache", Path.GetRandomFileName()),
                Usage = PathUsage.Public,
                Hash = FileHashAlgorithm.SHA256,
                Provider = FileSystemProvider.Local
            }
        };
        Assert.True((await path.IsPathWritable()).writable, "path is not writable");
        var filename = Path.GetRandomFileName();
        var content = "MewtLocalFileSystemPathTest.PathLifeCycleTest()";
        await path.WriteFile(filename, content);
        Assert.True(await path.DoesFileExist(filename));
        Assert.Equivalent(content, await path.ReadFile(filename));
        await path.DeleteFile(filename);
    }
}