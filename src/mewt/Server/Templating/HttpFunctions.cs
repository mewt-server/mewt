/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Scriban.Runtime;

namespace Mewt.Server.Templating;

public class HttpFunctions : ScriptObject
{
    /// <summary>
    /// Gets the Body of a HttpRequest as a string.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>Body as a string.</returns>
    public static async ValueTask<string> Body(HttpRequest request)
    {
        if (!request.Body.CanSeek)
            request.EnableBuffering();
        return await StreamFunctions.Read(request.Body);
    }

    /// <summary>
    /// Gets the Content of a HttpResponseMessage as a string.
    /// </summary>
    /// <param name="response">The response message.</param>
    /// <returns>Content as a string.</returns>
    public static async ValueTask<string> Content(HttpResponseMessage response)
        => await StreamFunctions.Read(response.Content.ReadAsStream());

    /// <summary>
    /// Sets the Content of a HttpRequestMessage from a string.
    /// </summary>
    /// <param name="request">The request message.</param>
    /// <param name="content">The content string.</param>
    public static void SetContent(HttpRequestMessage request, string content)
        => request.Content = new StringContent(content);

    /// <summary>
    /// Sets the Content of a HttpRequestMessage from a object.
    /// </summary>
    /// <param name="request">The request message.</param>
    /// <param name="content">The content object.</param>
    public static void SetContent(HttpRequestMessage request, object content)
        => request.Content = JsonContent.Create(content);

    /// <summary>
    /// Creates a new HttpMethod from a method string.
    /// </summary>
    /// <param name="method">The method string.</param>
    /// <returns>The HttpMethod.</returns>
    public static HttpMethod Method(string method)
        => new HttpMethod(method);

    /// <summary>
    /// Creates a new Uri from a string uri.
    /// </summary>
    /// <param name="url">The string uri.</param>
    /// <returns>The Uri.</returns>
    public static Uri Uri(string url)
        => new Uri(url);

    /// <summary>
    /// Creates a new HttpRequestMessage from a string method and a string uri.
    /// </summary>
    /// <param name="method">The string method.</param>
    /// <param name="url">The string uri.</param>
    /// <returns>The HttpRequestMessage.</returns>
    public static HttpRequestMessage CreateRequest(string method, string url)
        => new HttpRequestMessage(new HttpMethod(method), url);

    /// <summary>
    /// Sends a HttpRequestMessage.
    /// </summary>
    /// <param name="request">The HttpRequestMessage.</param>
    /// <returns>The HttpResponseMessage returned by the call.</returns>
    public static async ValueTask<HttpResponseMessage> SendRequest(HttpRequestMessage request)
    {
        using var handler = new HttpClientHandler();
        using var client = new HttpClient(handler);
        return await client.SendAsync(request);
    }
}