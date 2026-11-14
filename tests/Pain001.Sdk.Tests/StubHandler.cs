// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Pain001.Sdk.Tests;

/// <summary>What the client sent, captured before the request is disposed.</summary>
internal sealed record SentRequest(
    HttpMethod Method,
    Uri? Uri,
    string? Authorization,
    string? ContentType,
    string? Body);

/// <summary>An in-memory HTTP handler: no network, a canned answer.</summary>
internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<CancellationToken, Task<HttpResponseMessage>> _respond;

    internal StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond)
    {
        _respond = respond;
    }

    /// <summary>The last request the client sent.</summary>
    internal SentRequest? Last { get; private set; }

    /// <summary>A handler that answers every request with this status and body.</summary>
    internal static StubHandler Answer(HttpStatusCode status, string body, string mediaType = "application/json") =>
        new(_ => Task.FromResult(Response(status, body, mediaType)));

    /// <summary>A handler that answers every request with this status and these exact bytes.</summary>
    internal static StubHandler AnswerBytes(int status, byte[] body, string mediaType) =>
        new(_ => Task.FromResult(Response(status, body, mediaType)));

    // The response is returned to HttpClient, which owns and disposes it.
    private static HttpResponseMessage Response(HttpStatusCode status, string body, string mediaType) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    private static HttpResponseMessage Response(int status, byte[] body, string mediaType)
    {
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return new HttpResponseMessage((HttpStatusCode)status) { Content = content };
    }

    /// <summary>A handler that never answers, until the request is cancelled.</summary>
    internal static StubHandler Hang() =>
        new(async token =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException("unreachable");
        });

    /// <summary>A client sending through <paramref name="http"/>.</summary>
    internal static Pain001Client Client(HttpClient http, string? apiKey = null, TimeSpan? timeout = null) =>
        new(http, new Pain001ClientOptions
        {
            BaseAddress = new Uri("http://pain001.test:8000/"),
            ApiKey = apiKey,
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Last = new SentRequest(
            request.Method,
            request.RequestUri,
            request.Headers.Authorization?.ToString(),
            request.Content?.Headers.ContentType?.MediaType,
            body);
        return await _respond(cancellationToken);
    }
}
