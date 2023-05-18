/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace test;

public class MewtServerTest2
{
    private readonly IMewtServer _mewt;
    private readonly ITestOutputHelper _output;
    private readonly string _root;

    public MewtServerTest2(ITestOutputHelper output)
    {
        _output = output;
        // Init
        _root = Environment.CurrentDirectory;
        var builder = new ConfigurationBuilder().AddYamlFile(Path.Combine(_root, "mewt.yml"), optional: false);
        var config = builder.Build();
        // Make the server
        _mewt = new MewtServer(config, new XunitLogger<MewtServer>(_output), _root);
    }

    [Fact]
    public async Task HealthCheckTest()
    {
        var checks = await _mewt.HealthCheck();
        Assert.Empty(checks.Where(check => !check.Value.Success));
    }

    [Fact]
    public async Task FileNotFoundTest()
    {
        var sfi = await _mewt.CheckFile("does/not/exist.html");
        Assert.False(sfi.Exists);
        Assert.Equivalent(FileType.NotFound, sfi.Type);
    }

    [Theory]
    [InlineData("empty.txt")]
    [InlineData("notempty.txt")]
    public async Task AssetTest(string relativePath)
    {
        var sfi = await _mewt.CheckFile(relativePath);
        Assert.False(sfi.Exists);
        Assert.Equivalent(FileType.Asset, sfi.Type);
        var copy = await _mewt.CopyAsset(sfi);
        Assert.True(copy);
        var file = await _mewt.GetPublicFile(sfi);
        Assert.True(file.isPhysical);
        Assert.NotNull(file.absolutePath);
        Assert.True(File.Exists(file.absolutePath));
        var expected = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "data", "source", "assets", relativePath));
        var actual = await File.ReadAllTextAsync(file.absolutePath);
        Assert.Equivalent(expected, actual);
        var sfi2 = await _mewt.CheckFile(relativePath);
        Assert.False(sfi2.Exists);
    }

    [Theory]
    [InlineData("full.html")]
    [InlineData("include.html")]
    [InlineData("merge.html")]
    public async Task PageTest(string relativePath)
    {
        var sfi = await _mewt.CheckFile(relativePath);
        Assert.False(sfi.Exists);
        Assert.Equivalent(FileType.Page, sfi.Type);
        var generate = await _mewt.GeneratePage(sfi);
        Assert.True(generate);
        var file = await _mewt.GetPublicFile(sfi);
        Assert.False(file.isPhysical);
        var expected = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "data", "expected", relativePath));
        Assert.Equivalent(expected, file.content);
        var sfi2 = await _mewt.CheckFile(relativePath);
        Assert.True(sfi2.Exists);
        var clean = await _mewt.DeleteFile(FileType.Page, relativePath);
        Assert.True(clean);
        var sfi3 = await _mewt.CheckFile(relativePath);
        Assert.False(sfi3.Exists);
    }

    [Theory]
    [InlineData("randomuser", "GET", ApiStatus.Success, "email")]
    [InlineData("randomuser", "POST", ApiStatus.Forbidden, "")]
    public async Task ApiProxyTest(string path, string method, ApiStatus status, string expected)
    {
        var sai = await _mewt.CheckApi(ApiType.Proxy, path);
        Assert.False(sai.Exists);
        Assert.Equivalent(ApiType.Proxy, sai.Type);
        var copy = await _mewt.CopyApi(sai);
        Assert.True(copy);
        var sai2 = await _mewt.CheckApi(ApiType.Proxy, path);
        Assert.True(sai2.Exists);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        var api = await _mewt.ExecuteApi(sai, httpContext.Request, httpContext.Response);
        Assert.Equivalent(status, api.status);
        if (status == ApiStatus.Success)
        {
            Assert.NotNull(api.body);
            Assert.IsType<string>(api.body);
            var content = (api.body as string) ?? string.Empty;
            Assert.Contains(expected, content);
        }
    }
}