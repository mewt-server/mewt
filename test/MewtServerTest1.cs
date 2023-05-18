/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace test;

public class MewtServerTest1
{
    private readonly IMewtServer _mewt;
    private readonly ITestOutputHelper _output;
    private readonly string _root;

    public MewtServerTest1(ITestOutputHelper output)
    {
        _output = output;
        // Init
        _root = Path.Combine(Environment.CurrentDirectory, "data");
        var builder = new ConfigurationBuilder().AddYamlFile(Path.Combine(_root, "mewt.1.yml"), optional: false);
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
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "data", "cache", "public", relativePath)))
            File.Delete(Path.Combine(Environment.CurrentDirectory, "data", "cache", "public", relativePath));
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "data", "cache", "metadata", relativePath + ".yml")))
            File.Delete(Path.Combine(Environment.CurrentDirectory, "data", "cache", "metadata", relativePath + ".yml"));
        var filename = Path.Combine(Environment.CurrentDirectory, "data", "source", "assets", relativePath);
        Assert.True(File.Exists(filename));
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
        Assert.True(sfi2.Exists);
        /*var clean = await _mewt.DeleteFile(FileType.Asset, relativePath);
        Assert.True(clean);
        var sfi3 = await _mewt.CheckFile(relativePath);
        Assert.False(sfi3.Exists);*/
    }

    [Theory]
    [InlineData("full.html")]
    [InlineData("include.html")]
    [InlineData("merge.html")]
    public async Task PageTest(string relativePath)
    {
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "data", "cache", "public", relativePath)))
            File.Delete(Path.Combine(Environment.CurrentDirectory, "data", "cache", "public", relativePath));
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "data", "cache", "metadata", relativePath + ".yml")))
            File.Delete(Path.Combine(Environment.CurrentDirectory, "data", "cache", "metadata", relativePath + ".yml"));
        var sfi = await _mewt.CheckFile(relativePath);
        Assert.False(sfi.Exists);
        Assert.Equivalent(FileType.Page, sfi.Type);
        var generate = await _mewt.GeneratePage(sfi);
        Assert.True(generate);
        var file = await _mewt.GetPublicFile(sfi);
        Assert.True(file.isPhysical);
        Assert.NotNull(file.absolutePath);
        Assert.True(File.Exists(file.absolutePath));
        var expected = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "data", "expected", relativePath));
        var actual = await File.ReadAllTextAsync(file.absolutePath);
        Assert.Equivalent(expected, actual);
        var sfi2 = await _mewt.CheckFile(relativePath);
        Assert.True(sfi2.Exists);
        /*var clean = await _mewt.DeleteFile(FileType.Page, relativePath);
        Assert.True(clean);
        var sfi3 = await _mewt.CheckFile(relativePath);
        Assert.False(sfi3.Exists);*/
    }

    [Fact]
    public async Task MetricsTest()
    {
        var metrics = await _mewt.GetMetrics();
        Assert.True(metrics.ContainsKey("ApisFiles"));
        Assert.True(metrics.ContainsKey("AssetsFiles"));
        Assert.True(metrics.ContainsKey("ContentsFiles"));
        Assert.True(metrics.ContainsKey("MetadataFiles"));
        Assert.True(metrics.ContainsKey("PagesFiles"));
        Assert.True(metrics.ContainsKey("PrivateFiles"));
        Assert.True(metrics.ContainsKey("PublicFiles"));
        Assert.True(metrics.ContainsKey("TemplatesFiles"));
        Assert.True(metrics.ContainsKey("ApisBytes"));
        Assert.True(metrics.ContainsKey("AssetsBytes"));
        Assert.True(metrics.ContainsKey("ContentsBytes"));
        Assert.True(metrics.ContainsKey("MetadataBytes"));
        Assert.True(metrics.ContainsKey("PagesBytes"));
        Assert.True(metrics.ContainsKey("PrivateBytes"));
        Assert.True(metrics.ContainsKey("PublicBytes"));
        Assert.True(metrics.ContainsKey("TemplatesBytes"));
        Assert.True(metrics.ContainsKey("TotalProcessorTimeSeconds"));
        Assert.True(metrics.ContainsKey("ResidentMemoryBytes"));
        Assert.True(metrics.ContainsKey("ProcessStartTimeSeconds"));
        Assert.True(metrics.ContainsKey("VirtualMemoryBytes"));
    }

    [Theory]
    [InlineData("randomuser", "GET", ApiStatus.Success, "email")]
    [InlineData("randomuser", "POST", ApiStatus.Forbidden, "")]
    public async Task ApiProxyTest(string path, string method, ApiStatus status, string expected)
    {
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "data", "cache", "private", "api", "proxy", path + ".yml")))
            File.Delete(Path.Combine(Environment.CurrentDirectory, "data", "cache", "private", "api", "proxy", path + ".yml"));
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "data", "cache", "metadata", "api", "proxy", path + ".yml")))
            File.Delete(Path.Combine(Environment.CurrentDirectory, "data", "cache", "metadata", "api", "proxy", path + ".yml"));
        var filename = Path.Combine(Environment.CurrentDirectory, "data", "source", "apis", "proxy", path + ".yml");
        Assert.True(File.Exists(filename));
        var sai = await _mewt.CheckApi(ApiType.Proxy, path);
        Assert.False(sai.Exists);
        Assert.Equivalent(ApiType.Proxy, sai.Type);
        var copy = await _mewt.CopyApi(sai);
        Assert.True(copy);
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
        var sai2 = await _mewt.CheckApi(ApiType.Proxy, path);
        Assert.True(sai2.Exists);
    }
}