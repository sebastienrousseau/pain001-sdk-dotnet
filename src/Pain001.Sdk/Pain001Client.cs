// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Pain001.Sdk.Internal;

namespace Pain001.Sdk;

/// <summary>
/// A typed client for the pain001 REST API (<c>pain001 serve</c>).
/// </summary>
/// <remarks>
/// Every call is one HTTP request unless
/// <see cref="Pain001ClientOptions.Retry"/> is set, and then only a
/// <c>GET</c> is retried; request bodies are never logged, because they
/// hold payment data. A status
/// outside 2xx raises <see cref="Pain001ApiException"/> (a
/// <see cref="Pain001GenerationRefusedException"/>, with the findings,
/// when <see cref="GenerateAsync"/> is refused), except from
/// <see cref="RequestAsync"/>, which returns it.
/// </remarks>
public sealed class Pain001Client : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly string _baseAddress;
    private readonly string? _apiKey;
    private readonly TimeSpan _timeout;
    private readonly RetryPolicy? _retry;
    private readonly TimeProvider _time = TimeProvider.System;

    /// <summary>
    /// Creates a client that sends its requests through
    /// <paramref name="http"/>, which the caller owns and disposes. The
    /// client never changes the <see cref="HttpClient"/>'s own settings.
    /// </summary>
    /// <param name="http">The HTTP client to send requests with.</param>
    /// <param name="options">The server address, API key and timeout.</param>
    /// <exception cref="ArgumentException">The base address is not absolute,
    /// or the timeout is not positive.</exception>
    public Pain001Client(HttpClient http, Pain001ClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);
        _http = http;
        _baseAddress = CheckBaseAddress(options.BaseAddress);
        _apiKey = options.ApiKey;
        _timeout = CheckTimeout(options.Timeout);
        _retry = options.Retry;
        _time = options.TimeProvider;
    }

    /// <summary>
    /// Creates a client with its own <see cref="HttpClient"/>, disposed
    /// with the client, and the default 30-second timeout.
    /// </summary>
    /// <param name="baseAddress">The server's absolute base address.</param>
    /// <param name="apiKey">The API key, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException">The base address is not absolute.</exception>
    public Pain001Client(Uri baseAddress, string? apiKey = null)
    {
        _baseAddress = CheckBaseAddress(baseAddress);
        _apiKey = apiKey;
        _timeout = new Pain001ClientOptions().Timeout;
        _http = new HttpClient();
        _ownsHttp = true;
    }

    /// <summary>Checks the server is up (<c>GET /api/v1/health</c>).</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The server's status and version.</returns>
    /// <exception cref="Pain001ApiException">The server answered outside 2xx.</exception>
    public Task<HealthResponse> HealthAsync(CancellationToken cancellationToken = default) =>
        SendAsync<HealthResponse>(HttpMethod.Get, "/api/v1/health", null, cancellationToken);

    /// <summary>
    /// Validates a payment file without generating XML
    /// (<c>POST /api/v1/ui/validate</c>).
    /// </summary>
    /// <param name="content">The file's text: CSV, JSON or JSON Lines.</param>
    /// <param name="filename">The file name; only its suffix picks the parser.</param>
    /// <param name="messageType">The message type, for example
    /// <see cref="MessageTypes.Pain001V09"/>.</param>
    /// <param name="scheme">Scheme rulebooks to enforce, comma-separated, or
    /// <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The row counts and every error.</returns>
    /// <exception cref="Pain001ApiException">The server answered outside 2xx,
    /// for example HTTP 400 for a row it rejects.</exception>
    public Task<ValidationResponse> ValidateAsync(
        string content,
        string filename,
        string messageType,
        string? scheme = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<ValidationResponse>(
            HttpMethod.Post,
            "/api/v1/ui/validate",
            FilePayload(content, filename, messageType, scheme),
            cancellationToken);

    /// <summary>
    /// Generates the XML document from a payment file
    /// (<c>POST /api/v1/ui/generate</c>).
    /// </summary>
    /// <param name="content">The file's text: CSV, JSON or JSON Lines.</param>
    /// <param name="filename">The file name; only its suffix picks the parser.</param>
    /// <param name="messageType">The message type, for example
    /// <see cref="MessageTypes.Pain001V09"/>.</param>
    /// <param name="scheme">Scheme rulebooks to enforce, comma-separated, or
    /// <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The generated XML and its suggested file name.</returns>
    /// <exception cref="Pain001GenerationRefusedException">The server refused
    /// the data (HTTP 422 with <c>success: false</c>); its
    /// <see cref="Pain001GenerationRefusedException.Response"/> holds the
    /// findings.</exception>
    /// <exception cref="Pain001ApiException">The server answered outside 2xx
    /// for another reason, for example HTTP 422 for an unknown message
    /// type.</exception>
    /// <exception cref="Pain001NoDocumentException">The server answered 2xx
    /// without the XML (a server older than pain001 0.0.87, or a broken
    /// one).</exception>
    public Task<UiGenerateResponse> GenerateAsync(
        string content,
        string filename,
        string messageType,
        string? scheme = null,
        CancellationToken cancellationToken = default) =>
        GenerateCoreAsync(FilePayload(content, filename, messageType, scheme), cancellationToken);

    /// <summary>
    /// Runs the validation ladder on an existing pain.001 or pain.008
    /// document (<c>POST /api/v1/check</c>).
    /// </summary>
    /// <param name="xml">The document as XML text.</param>
    /// <param name="scheme">Scheme profiles to add, comma-separated, or
    /// <see langword="null"/>.</param>
    /// <param name="schematron">A Schematron preset to add (<c>sepa</c>,
    /// <c>fednow</c>, <c>cbpr</c>), or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The verdict, the rungs that ran and every finding.</returns>
    /// <exception cref="Pain001ApiException">The server answered outside 2xx,
    /// HTTP 422 when the document cannot be checked.</exception>
    public Task<CheckResponse> CheckAsync(
        string xml,
        string? scheme = null,
        string? schematron = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xml);
        return SendAsync<CheckResponse>(
            HttpMethod.Post,
            "/api/v1/check",
            new CheckRequest(xml, scheme, schematron),
            cancellationToken);
    }

    /// <summary>Lists the faces (<c>GET /api/v1/faces</c>).</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>One entry per face, sorted by name.</returns>
    /// <exception cref="Pain001ApiException">The server answered outside 2xx.</exception>
    public Task<IReadOnlyList<FaceInfo>> FacesAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<FaceInfo>>(HttpMethod.Get, "/api/v1/faces", null, cancellationToken);

    /// <summary>
    /// Projects a pain.001 document into a face
    /// (<c>POST /api/v1/faces/{face}</c>).
    /// </summary>
    /// <param name="face">The face name, one of <see cref="FacesAsync"/>.</param>
    /// <param name="xml">The pain.001 document as XML text.</param>
    /// <param name="version">The pain.001 edition, or <see langword="null"/>
    /// to detect it from the namespace.</param>
    /// <param name="bindings">Side inputs the message does not carry, or
    /// <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The payload and its loss report.</returns>
    /// <exception cref="Pain001ApiException">The server answered outside 2xx,
    /// HTTP 404 for an unknown face, 422 when the face refuses the input.</exception>
    public Task<FaceResponse> ProjectFaceAsync(
        string face,
        string xml,
        string? version = null,
        IReadOnlyDictionary<string, object?>? bindings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(face);
        ArgumentNullException.ThrowIfNull(xml);
        return SendAsync<FaceResponse>(
            HttpMethod.Post,
            "/api/v1/faces/" + Uri.EscapeDataString(face),
            new FaceRequest(xml, version, bindings),
            cancellationToken);
    }

    /// <summary>
    /// Sends one request and returns its status and parsed body, whatever
    /// the status. This is the low-level call the conformance kit runs
    /// through.
    /// </summary>
    /// <param name="method">The HTTP method, for example <c>GET</c>.</param>
    /// <param name="path">The path from the base address, starting with
    /// <c>/</c>, for example <c>/api/v1/health</c>.</param>
    /// <param name="body">The JSON body, or <see langword="null"/> for none.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The status code and parsed body.</returns>
    /// <exception cref="ArgumentException">The path does not start with <c>/</c>.</exception>
    public Task<Pain001RawResponse> RequestAsync(
        string method,
        string path,
        JsonNode? body = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path[0] != '/')
        {
            throw new ArgumentException("The path must start with '/'.", nameof(path));
        }

        byte[]? json = body is null ? null : JsonSerializer.SerializeToUtf8Bytes(body, Wire.Options);
        return SendCoreAsync(new HttpMethod(method.ToUpperInvariant()), path, json, cancellationToken);
    }

    /// <summary>
    /// Disposes the <see cref="HttpClient"/> when this client created it;
    /// a caller's <see cref="HttpClient"/> is left alone.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private static UiFilePayload FilePayload(string content, string filename, string messageType, string? scheme)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(filename);
        ArgumentException.ThrowIfNullOrEmpty(messageType);
        return new UiFilePayload(content, filename, messageType, scheme);
    }

    private static string CheckBaseAddress(Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        if (!baseAddress.IsAbsoluteUri)
        {
            throw new ArgumentException("The base address must be an absolute URI.", nameof(baseAddress));
        }

        return baseAddress.AbsoluteUri.TrimEnd('/');
    }

    private static TimeSpan CheckTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentException("The timeout must be positive, or infinite.", nameof(timeout));
        }

        return timeout;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? payload, CancellationToken cancellationToken)
    {
        Pain001RawResponse response = await SendPayloadAsync(method, path, payload, cancellationToken).ConfigureAwait(false);
        return Wire.Decode<T>(response);
    }

    private async Task<UiGenerateResponse> GenerateCoreAsync(UiFilePayload payload, CancellationToken cancellationToken)
    {
        Pain001RawResponse response = await SendPayloadAsync(
            HttpMethod.Post, "/api/v1/ui/generate", payload, cancellationToken).ConfigureAwait(false);
        Pain001GenerationRefusedException? refusal = Wire.AsRefusal(response);
        if (refusal is not null)
        {
            throw refusal;
        }

        UiGenerateResponse result = Wire.Decode<UiGenerateResponse>(response);
        if (!result.Success || string.IsNullOrEmpty(result.Xml))
        {
            throw new Pain001NoDocumentException(response.StatusCode, result);
        }

        return result;
    }

    private Task<Pain001RawResponse> SendPayloadAsync(HttpMethod method, string path, object? payload, CancellationToken cancellationToken)
    {
        byte[]? json = payload is null ? null : JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), Wire.Options);
        return SendCoreAsync(method, path, json, cancellationToken);
    }

    private async Task<Pain001RawResponse> SendCoreAsync(
        HttpMethod method,
        string path,
        byte[]? json,
        CancellationToken cancellationToken)
    {
        if (_retry is null || method != HttpMethod.Get)
        {
            (Pain001RawResponse once, _) = await SendOnceAsync(method, path, json, cancellationToken).ConfigureAwait(false);
            return once;
        }

        return await SendWithRetryAsync(_retry, path, cancellationToken).ConfigureAwait(false);
    }

    // Only a GET reaches here: it has no body, and sending it again is safe.
    private async Task<Pain001RawResponse> SendWithRetryAsync(RetryPolicy retry, string path, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            TimeSpan? wait;
            try
            {
                (Pain001RawResponse response, RetryConditionHeaderValue? retryAfter) =
                    await SendOnceAsync(HttpMethod.Get, path, null, cancellationToken).ConfigureAwait(false);
                wait = retry.DelayAfter(attempt, response.StatusCode, retryAfter, _time.GetUtcNow());
                if (wait is null)
                {
                    return response;
                }
            }
            catch (HttpRequestException) when (attempt < retry.MaxAttempts)
            {
                wait = retry.Backoff(attempt);
            }

            await Task.Delay(wait.Value, _time, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<(Pain001RawResponse Response, RetryConditionHeaderValue? RetryAfter)> SendOnceAsync(
        HttpMethod method,
        string path,
        byte[]? json,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_timeout);
        using HttpRequestMessage request = BuildRequest(method, path, json);
        try
        {
            using HttpResponseMessage response = await _http.SendAsync(request, deadline.Token).ConfigureAwait(false);
            byte[] bytes = await response.Content.ReadAsByteArrayAsync(deadline.Token).ConfigureAwait(false);
            return (new Pain001RawResponse((int)response.StatusCode, Wire.ParseBody(bytes)), response.Headers.RetryAfter);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The pain001 API did not answer within the timeout.", ex);
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, byte[]? json)
    {
        var request = new HttpRequestMessage(method, new Uri(_baseAddress + path, UriKind.Absolute));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (_apiKey is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        if (json is not null)
        {
            request.Content = new ByteArrayContent(json);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        return request;
    }
}
