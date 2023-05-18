/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Microsoft.AspNetCore.Mvc.Testing;

namespace test;

public class MewtServerTestWAF : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MewtServerTestWAF(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("GET", "api/system/cache/status", 200, "application/json; charset=utf-8", "[]")]
    [InlineData("DELETE", "api/system/cache/all", 200, "application/json; charset=utf-8", "[]")]
    [InlineData("DELETE", "api/system/cache/file?type=public&file=index.html", 200, "application/json; charset=utf-8", "true")]
    [InlineData("DELETE", "api/system/cache/outdated?hash=false", 200, "application/json; charset=utf-8", "[]")]
    [InlineData("DELETE", "api/system/cache/outdated?hash=true", 200, "application/json; charset=utf-8", "[]")]
    [InlineData("GET", "api/system/config", 200, "text/plain; charset=utf-8", "server:paths: null")]
    [InlineData("POST", "api/system/path/update/public", 501, "text/plain; charset=utf-8", "server:paths:public:updateCommand is not defined.")]
    [InlineData("GET", "healthcheck", 200, "application/json; charset=utf-8", "IsPublicPathWriteable")]
    [InlineData("GET", "metrics", 200, "text/plain; charset=utf-8", "mewt_filesystem_files")]
    public async Task ApiCallTest(string method, string url, int expectedReturnCode, string expectedContentType, string? expectedContentPart)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        var response = await client.SendAsync(request);
        Assert.Equivalent(expectedReturnCode, (int)response.StatusCode);
        Assert.Equivalent(expectedContentType, response.Content.Headers.ContentType?.ToString());
        if (expectedContentPart != null)
            Assert.Contains(expectedContentPart, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("empty.txt", "text/plain", "")]
    [InlineData("notempty.txt", "text/plain", "It's not empty")]
    public async Task AssetsTest(string url, string expectedContentType, string expectedContentFull)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equivalent(200, (int)response.StatusCode);
        Assert.Equivalent(expectedContentType, response.Content.Headers.ContentType?.ToString());
        Assert.Equivalent(expectedContentFull, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("full.html", "text/html", "Full Body")]
    [InlineData("include.html", "text/html", "Include Body")]
    [InlineData("merge.html", "text/html", "Merge Body")]
    public async Task PagesTest(string url, string expectedContentType, string? expectedContentPart)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equivalent(200, (int)response.StatusCode);
        Assert.Equivalent(expectedContentType, response.Content.Headers.ContentType?.ToString());
        if (expectedContentPart != null)
            Assert.Contains(expectedContentPart, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("api/proxy/notfound")]
    [InlineData("api/system/notfound")]
    [InlineData("notfound")]
    [InlineData("notfound/notfound")]
    public async Task NotFoundTest(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equivalent(404, (int)response.StatusCode);
    }

    [Fact]
    public async Task LifeCycleTest()
    {
        var client = _factory.CreateClient();
        // Clean previous tests
        await client.DeleteAsync("api/system/cache/all");
        // Actual test
        var status0 = await client.GetAsync("api/system/cache/status");
        Assert.Equivalent("[]", await status0.Content.ReadAsStringAsync());
        Assert.Equivalent(200, (int)(await client.GetAsync("full.html")).StatusCode);
        Assert.Equivalent(200, (int)(await client.GetAsync("include.html")).StatusCode);
        Assert.Equivalent(200, (int)(await client.GetAsync("merge.html")).StatusCode);
        Assert.Equivalent(200, (int)(await client.GetAsync("empty.txt")).StatusCode);
        Assert.Equivalent(200, (int)(await client.GetAsync("notempty.txt")).StatusCode);
        await Task.Delay(100); // Make sure metadata are generated as we don't wait them normally
        var status1 = await client.GetAsync("api/system/cache/status");
        var status1Content = await status1.Content.ReadAsStringAsync();
        Assert.Contains("{\"type\":\"Page\",\"name\":\"full.html\",\"status\":\"UpToDate\"}", status1Content);
        Assert.Contains("{\"type\":\"Page\",\"name\":\"include.html\",\"status\":\"UpToDate\"}", status1Content);
        Assert.Contains("{\"type\":\"Page\",\"name\":\"merge.html\",\"status\":\"UpToDate\"}", status1Content);
        Assert.DoesNotContain("empty.txt", status1Content);
        Assert.DoesNotContain("notempty.txt", status1Content);
        Assert.Equivalent("[]", await (await client.DeleteAsync("api/system/cache/outdated?hash=false")).Content.ReadAsStringAsync());
        Assert.Equivalent("[]", await (await client.DeleteAsync("api/system/cache/outdated?hash=true")).Content.ReadAsStringAsync());
        var status2 = await client.GetAsync("api/system/cache/status");
        var status2Content = await status2.Content.ReadAsStringAsync();
        Assert.Contains("full.html", status2Content);
        Assert.Contains("include.html", status2Content);
        Assert.Contains("merge.html", status2Content);
        Assert.DoesNotContain("empty.txt", status2Content);
        Assert.DoesNotContain("notempty.txt", status2Content);
        Assert.Equivalent("true", await (await client.DeleteAsync("api/system/cache/file?type=public&file=full.html")).Content.ReadAsStringAsync());
        var status3 = await client.GetAsync("api/system/cache/status");
        var status3Content = await status3.Content.ReadAsStringAsync();
        Assert.DoesNotContain("full.html", status3Content);
        Assert.Contains("include.html", status3Content);
        Assert.Contains("merge.html", status3Content);
        Assert.DoesNotContain("empty.txt", status3Content);
        Assert.DoesNotContain("notempty.txt", status3Content);
        Assert.Equivalent("[]", await (await client.DeleteAsync("api/system/cache/all")).Content.ReadAsStringAsync());
        Assert.Equivalent("[]", await (await client.GetAsync("api/system/cache/status")).Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("GET", "api/proxy/add?a=1&b=2", 200, "1 + 2 = 3")]
    [InlineData("GET", "api/proxy/curl?url=https://jsonplaceholder.typicode.com/posts/1", 200, "body")]
    [InlineData("POST", "api/proxy/curl?url=https://jsonplaceholder.typicode.com/posts/1", 500, "SyntaxError")]
    [InlineData("GET", "api/proxy/dotnet", 200, "7")]
    [InlineData("GET", "api/proxy/randomuser", 200, "email")]
    [InlineData("POST", "api/proxy/randomuser", 405, "")]
    public async Task ApiProxyTest(string method, string url, int expectedReturnCode, string expectedContentPart)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        var response = await client.SendAsync(request);
        Assert.Equivalent(expectedReturnCode, (int)response.StatusCode);
        if (expectedContentPart != null)
            Assert.Contains(expectedContentPart, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(true, 403)]
    [InlineData(false, 200)]
    public async Task ValidateRequestTest(bool addRefuse, int expectedReturnCode)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod("GET"), "full.html");
        if (addRefuse)
            request.Headers.Add("X-Refuse-Request", "yes");
        var response = await client.SendAsync(request);
        Assert.Equivalent(expectedReturnCode, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(PathUsage.Apis, 200)]
    [InlineData(PathUsage.Assets, 200)]
    [InlineData(PathUsage.Metadata, 501)]
    public async Task UpdatePathTest(PathUsage path, int expectedReturnCode)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"api/system/path/update/{path.ToString().ToLowerInvariant()}", null);
        Assert.Equivalent(expectedReturnCode, (int)response.StatusCode);
        if (expectedReturnCode == 200)
            Assert.Contains(Path.Combine(Environment.CurrentDirectory, $"data/source/{path.ToString().ToLowerInvariant()}"), await response.Content.ReadAsStringAsync());
    }
}