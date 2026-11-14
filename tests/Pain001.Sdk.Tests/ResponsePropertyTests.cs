// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using FsCheck;
using FsCheck.Xunit;

namespace Pain001.Sdk.Tests;

/// <summary>
/// Property tests over what a server sends back, the client's untrusted
/// input: FsCheck generates statuses, bodies and media types and drives
/// them through the stub handler.
/// </summary>
public sealed class ResponsePropertyTests
{
    private const int Cases = 500;

    // The whitespace JSON allows around a value (RFC 8259, section 2).
    private static readonly char[] Whitespace = [' ', '\t', '\n', '\r'];

    // The typed methods, each reading one answer.
    private static readonly string[] Methods = ["health", "validate", "generate", "check", "faces", "project"];

    /// <summary>
    /// Whatever the server sends, a typed method either decodes it or
    /// raises one of two exceptions: <see cref="Pain001ApiException"/> for
    /// a status outside 2xx, carrying that status, or
    /// <see cref="JsonException"/> for a 2xx body that is not the expected
    /// shape. Nothing else escapes.
    /// </summary>
    [Property(MaxTest = Cases, Arbitrary = [typeof(ServerArbitraries)])]
    public async Task TypedMethodsRaiseOnlyTheDocumentedExceptions(ServerAnswer answer)
    {
        foreach (string method in Methods)
        {
            Exception? error = await Record.ExceptionAsync(() => Send(answer, method));
            AssertDocumentedOutcome(answer.Status, error);
        }
    }

    /// <summary>
    /// The low-level call never throws on an answer: it returns the status
    /// and the body, parsed when it is JSON and kept as text when not.
    /// </summary>
    [Property(MaxTest = Cases, Arbitrary = [typeof(ServerArbitraries)])]
    public async Task RequestReturnsEveryAnswerAsIs(ServerAnswer answer)
    {
        using var handler = StubHandler.AnswerBytes(answer.Status, answer.Body.ToArray(), answer.MediaType);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        Pain001RawResponse response = await client.RequestAsync("GET", "/api/v1/health");

        Assert.Equal(answer.Status, response.StatusCode);
        AssertBodyKept(answer.Body, response.Body);
    }

    /// <summary>
    /// An error's detail can echo payment data: the exception message is
    /// the status alone, and the detail is kept intact for the caller.
    /// </summary>
    [Property(MaxTest = Cases, Arbitrary = [typeof(ServerArbitraries)])]
    public async Task AnErrorDetailNeverReachesTheMessage(ErrorAnswer answer)
    {
        (byte[] body, string mediaType) = ErrorBody(answer);
        using var handler = StubHandler.AnswerBytes(answer.Status, body, mediaType);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.HealthAsync());

        Assert.Equal(answer.Status, error.StatusCode);
        Assert.Equal(ExpectedMessage(answer.Status), error.Message);
        Assert.DoesNotContain(answer.Secret[..18], error.ToString(), StringComparison.Ordinal);
        Assert.Contains(answer.Secret[..18], error.Detail.GetRawText(), StringComparison.Ordinal);
    }

    /// <summary>A well-formed answer decodes to the values it holds.</summary>
    [Property(MaxTest = Cases, Arbitrary = [typeof(ServerArbitraries)])]
    public async Task AWellFormedAnswerRoundTrips(TypedAnswer answer)
    {
        using var handler = StubHandler.AnswerBytes(200, Encoding.UTF8.GetBytes(answer.Json), "application/json");
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        object decoded = answer.Method switch
        {
            "health" => await client.HealthAsync(),
            "validate" => await client.ValidateAsync("x", "p.csv", MessageTypes.Pain001V09),
            "check" => await client.CheckAsync("<x/>"),
            _ => await client.FacesAsync(),
        };

        Assert.Equal(answer.Json, JsonSerializer.Serialize(decoded, decoded.GetType()));
    }

    /// <summary>The checker itself: a property that is false is reported as failing.</summary>
    [Fact]
    public void AFalsePropertyFails()
    {
        Property everyStatusIsSuccess = FsCheck.Fluent.Prop.ForAll(
            ServerArbitraries.Answer(),
            answer => answer.Status is >= 200 and <= 299);

        Assert.ThrowsAny<Exception>(() => Check.One(Config.QuickThrowOnFailure.WithMaxTest(Cases), everyStatusIsSuccess));
    }

    private static async Task Send(ServerAnswer answer, string method)
    {
        using var handler = StubHandler.AnswerBytes(answer.Status, answer.Body.ToArray(), answer.MediaType);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);
        await Call(client, method);
    }

    private static Task Call(Pain001Client client, string method) => method switch
    {
        "health" => client.HealthAsync(),
        "validate" => client.ValidateAsync("x", "p.csv", MessageTypes.Pain001V09),
        "generate" => client.GenerateAsync("x", "p.csv", MessageTypes.Pain001V09),
        "check" => client.CheckAsync("<x/>"),
        "faces" => client.FacesAsync(),
        _ => client.ProjectFaceAsync("uk_open_banking", "<x/>"),
    };

    private static void AssertDocumentedOutcome(int status, Exception? error)
    {
        bool success = status is >= 200 and <= 299;
        switch (error)
        {
            case null:
                Assert.True(success, $"HTTP {status} decoded instead of raising");
                break;
            case Pain001ApiException api:
                Assert.False(success, $"HTTP {status} raised Pain001ApiException");
                Assert.Equal(status, api.StatusCode);
                AssertReadable(api.Detail);
                break;
            case JsonException:
                Assert.True(success, $"HTTP {status} raised JsonException instead of Pain001ApiException");
                break;
            default:
                Assert.Fail($"HTTP {status} raised an undocumented {error.GetType()}: {error}");
                break;
        }
    }

    // A caller's error handler reads the detail; reading it must not throw.
    private static void AssertReadable(JsonElement detail)
    {
        if (detail.ValueKind != JsonValueKind.Undefined)
        {
            Assert.NotNull(detail.GetRawText());
        }
    }

    // JSON (valid UTF-8 that parses) comes back as sent; anything else
    // comes back as a string of its text. Either way the element can be
    // read: GetRawText transcodes every byte of it.
    private static void AssertBodyKept(ReadOnlyMemory<byte> sent, JsonElement body)
    {
        if (sent.IsEmpty)
        {
            Assert.Equal(JsonValueKind.Undefined, body.ValueKind);
            return;
        }

        if (IsJson(sent))
        {
            Assert.Equal(Encoding.UTF8.GetString(sent.Span).Trim(Whitespace), body.GetRawText());
            return;
        }

        Assert.Equal(JsonValueKind.String, body.ValueKind);
        Assert.Equal(Encoding.UTF8.GetString(sent.Span), body.GetString());
    }

    private static bool IsJson(ReadOnlyMemory<byte> bytes)
    {
        if (!Utf8.IsValid(bytes.Span))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(bytes);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static (byte[] Body, string MediaType) ErrorBody(ErrorAnswer answer)
    {
        object? json = answer.Shape switch
        {
            DetailShape.Text => new { detail = answer.Secret },
            DetailShape.ValidationArray => new { detail = new[] { new { loc = new[] { "body", "content" }, msg = answer.Secret, type = "value_error", input = answer.Secret } } },
            DetailShape.WholeBody => new { error = answer.Secret },
            _ => null,
        };
        return json is null
            ? (Encoding.UTF8.GetBytes("<html>" + answer.Secret + "</html>"), "text/html")
            : (JsonSerializer.SerializeToUtf8Bytes(json), "application/json");
    }

    private static string ExpectedMessage(int status) =>
        string.Create(CultureInfo.InvariantCulture, $"The pain001 API returned HTTP {status}; see Detail for the server's reason.");
}
