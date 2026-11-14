// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pain001.Sdk.Tests;

/// <summary>The client against a stub handler: no server needed.</summary>
public sealed class ClientTests
{
    private const string HealthBody = """{"status":"healthy","version":"0.0.86","message":"ok"}""";

    [Fact]
    public async Task HealthSendsAGetAndReadsTheTypedFields()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.OK, HealthBody);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        HealthResponse health = await client.HealthAsync();

        Assert.Equal("healthy", health.Status);
        Assert.Equal("0.0.86", health.Version);
        Assert.Equal("ok", health.Message);
        Assert.Equal(HttpMethod.Get, handler.Last!.Method);
        Assert.Equal(new Uri("http://pain001.test:8000/api/v1/health"), handler.Last.Uri);
        Assert.Null(handler.Last.Body);
    }

    [Fact]
    public async Task TheApiKeyIsSentAsABearerToken()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.OK, HealthBody);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http, apiKey: "test-key");

        await client.HealthAsync();

        Assert.Equal("Bearer test-key", handler.Last!.Authorization);
    }

    [Fact]
    public async Task NoApiKeyMeansNoAuthorizationHeader()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.OK, HealthBody);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        await client.HealthAsync();

        Assert.Null(handler.Last!.Authorization);
    }

    [Fact]
    public async Task ABasePathPrefixIsKept()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.OK, HealthBody);
        using var http = new HttpClient(handler);
        var options = new Pain001ClientOptions { BaseAddress = new Uri("https://example.test/payments/") };
        using var client = new Pain001Client(http, options);

        await client.HealthAsync();

        Assert.Equal(new Uri("https://example.test/payments/api/v1/health"), handler.Last!.Uri);
    }

    [Fact]
    public async Task ValidatePostsTheFilePayloadInSnakeCase()
    {
        const string Answer = """{"is_valid":true,"total_rows":5,"valid_rows":5,"invalid_rows":0,"errors":[]}""";
        using var handler = StubHandler.Answer(HttpStatusCode.OK, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        ValidationResponse result = await client.ValidateAsync("id,date", "payments.csv", MessageTypes.Pain001V09);

        Assert.True(result.IsValid);
        Assert.Equal(5, result.TotalRows);
        Assert.Empty(result.Errors!);
        Assert.Null(result.SchemeViolations);
        Assert.Equal(HttpMethod.Post, handler.Last!.Method);
        Assert.Equal("application/json", handler.Last.ContentType);
        Assert.Equal(
            """{"content":"id,date","filename":"payments.csv","message_type":"pain.001.001.09"}""",
            handler.Last.Body);
    }

    [Fact]
    public async Task RowErrorsUsePain001sShape()
    {
        const string Answer = """
            {"is_valid":false,"total_rows":2,"valid_rows":1,"invalid_rows":1,
             "errors":[{"field":"creditor_account_IBAN","message":"not a valid IBAN","value":null}]}
            """;
        using var handler = StubHandler.Answer(HttpStatusCode.OK, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        ValidationResponse result = await client.ValidateAsync("x", "p.csv", MessageTypes.Pain001V09);

        RowError error = Assert.Single(result.Errors!);
        Assert.Equal("creditor_account_IBAN", error.Field);
        Assert.Equal("not a valid IBAN", error.Message);
        Assert.Equal(1, result.InvalidRows);
    }

    [Fact]
    public async Task A422DetailDeserializesAsValidationErrors()
    {
        const string Answer = """{"detail":[{"loc":["body","message_type"],"msg":"bad","type":"enum","input":"pain.999"}]}""";
        using var handler = StubHandler.Answer(HttpStatusCode.UnprocessableEntity, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.HealthAsync());
        List<ValidationError> items = error.Detail.Deserialize<List<ValidationError>>()!;

        ValidationError item = Assert.Single(items);
        Assert.Equal("enum", item.ErrorType);
        Assert.Equal("message_type", item.Location[1].GetString());
    }

    [Fact]
    public async Task GenerateSendsTheSchemeWhenGiven()
    {
        const string Answer = """{"success":true,"message":"done","filename":"payments.xml","xml":"<Document/>"}""";
        using var handler = StubHandler.Answer(HttpStatusCode.OK, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        UiGenerateResponse result = await client.GenerateAsync("x", "p.csv", "pain.001.001.03", scheme: "sepa-sct");

        Assert.True(result.Success);
        Assert.Equal("<Document/>", result.Xml);
        Assert.Equal("payments.xml", result.Filename);
        Assert.Equal(new Uri("http://pain001.test:8000/api/v1/ui/generate"), handler.Last!.Uri);
        Assert.Contains("\"scheme\":\"sepa-sct\"", handler.Last.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckReadsFindings()
    {
        const string Answer = """
            {"message_type":"pain.001.001.09","passed":false,"errors":1,"warnings":0,"rungs":["xsd"],
             "findings":[{"rung":"xsd","rule":"r1","severity":"error","message":"bad","location":null}]}
            """;
        using var handler = StubHandler.Answer(HttpStatusCode.OK, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        CheckResponse result = await client.CheckAsync("<x/>", schematron: "sepa");

        Assert.False(result.Passed);
        Assert.Equal(1, result.Errors);
        Assert.Equal("xsd", Assert.Single(result.Rungs));
        CheckFinding finding = Assert.Single(result.Findings);
        Assert.Equal("r1", finding.Rule);
        Assert.Null(finding.Location);
        JsonNode sent = JsonNode.Parse(handler.Last!.Body!)!;
        Assert.Equal("<x/>", sent["xml"]!.GetValue<string>());
        Assert.Equal("sepa", sent["schematron"]!.GetValue<string>());
        Assert.False(sent.AsObject().ContainsKey("scheme"));
    }

    [Fact]
    public async Task FacesReadsTheList()
    {
        const string Answer = """[{"name":"berlin_group","description":"Berlin Group","bindings":[]}]""";
        using var handler = StubHandler.Answer(HttpStatusCode.OK, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        IReadOnlyList<FaceInfo> faces = await client.FacesAsync();

        FaceInfo face = Assert.Single(faces);
        Assert.Equal("berlin_group", face.Name);
        Assert.Empty(face.Bindings);
    }

    [Fact]
    public async Task ProjectFaceEscapesTheNameAndSendsBindings()
    {
        const string Answer = """
            {"face":"uk_open_banking","version":"pain.001.001.09","payload":{"Data":{}},
             "loss_report":{"face":"uk_open_banking","total_source_elements":4,"mapped_count":3,
             "unmapped_count":1,"is_lossless":false,"loss_ratio":0.25,"mapped_paths":["a","b","c"],
             "unmapped_paths":["d"]}}
            """;
        using var handler = StubHandler.Answer(HttpStatusCode.OK, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);
        var bindings = new Dictionary<string, object?> { ["ConsentId"] = "c-1" };

        FaceResponse result = await client.ProjectFaceAsync("uk open/banking", "<x/>", bindings: bindings);

        Assert.Equal("uk_open_banking", result.Face);
        Assert.Equal(JsonValueKind.Object, result.Payload.ValueKind);
        Assert.Equal(0.25, result.LossReport.LossRatio);
        Assert.Equal("d", Assert.Single(result.LossReport.UnmappedPaths));
        Assert.Equal("/api/v1/faces/uk%20open%2Fbanking", handler.Last!.Uri!.AbsolutePath);
        JsonNode sent = JsonNode.Parse(handler.Last.Body!)!;
        Assert.Equal("<x/>", sent["xml"]!.GetValue<string>());
        Assert.Equal("c-1", sent["bindings"]!["ConsentId"]!.GetValue<string>());
        Assert.False(sent.AsObject().ContainsKey("version"));
    }

    [Fact]
    public async Task AStringDetailBecomesAnApiException()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.BadRequest, """{"detail":"row 1: creditor_account_IBAN DE00 is not a valid IBAN"}""");
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(
            () => client.ValidateAsync("x", "p.csv", MessageTypes.Pain001V09));

        Assert.Equal(400, error.StatusCode);
        Assert.Equal(JsonValueKind.String, error.Detail.ValueKind);
        Assert.Contains("not a valid IBAN", error.Detail.GetString(), StringComparison.Ordinal);
        // Payment data stays out of the message, which ends up in logs.
        Assert.DoesNotContain("DE00", error.Message, StringComparison.Ordinal);
        Assert.Contains("400", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A422DetailArrayIsKept()
    {
        const string Answer = """{"detail":[{"loc":["body","message_type"],"msg":"bad","type":"enum"}]}""";
        using var handler = StubHandler.Answer(HttpStatusCode.UnprocessableEntity, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(
            () => client.GenerateAsync("x", "p.csv", "pain.999.001.01"));

        Assert.Equal(422, error.StatusCode);
        Assert.Equal(JsonValueKind.Array, error.Detail.ValueKind);
        Assert.Equal(1, error.Detail.GetArrayLength());
    }

    [Fact]
    public async Task ABodyWithoutDetailIsTheDetail()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.NotFound, """{"error":"gone"}""");
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.FacesAsync());

        Assert.Equal(404, error.StatusCode);
        Assert.Equal("gone", error.Detail.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ANonJsonErrorBodyIsKeptAsText()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.BadGateway, "<html>bad gateway</html>", "text/html");
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.HealthAsync());

        Assert.Equal(502, error.StatusCode);
        Assert.Equal("<html>bad gateway</html>", error.Detail.GetString());
    }

    [Fact]
    public async Task AnEmptySuccessBodyIsAJsonError()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.OK, string.Empty);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        await Assert.ThrowsAsync<JsonException>(() => client.HealthAsync());
    }

    [Fact]
    public async Task RequestReturnsTheStatusInsteadOfThrowing()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.UnprocessableEntity, """{"detail":"no"}""");
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);
        var body = new JsonObject { ["xml"] = "<x/>" };

        Pain001RawResponse response = await client.RequestAsync("post", "/api/v1/check", body);

        Assert.Equal(422, response.StatusCode);
        Assert.Equal("no", response.Body.GetProperty("detail").GetString());
        Assert.Equal(HttpMethod.Post, handler.Last!.Method);
        Assert.Equal("<x/>", JsonNode.Parse(handler.Last.Body!)!["xml"]!.GetValue<string>());
    }

    [Fact]
    public async Task RequestWithAnEmptyBodyIsUndefined()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.NoContent, string.Empty);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        Pain001RawResponse response = await client.RequestAsync("GET", "/api/v1/health");

        Assert.Equal(204, response.StatusCode);
        Assert.Equal(JsonValueKind.Undefined, response.Body.ValueKind);
    }

    [Fact]
    public async Task RequestRejectsAPathWithoutALeadingSlash()
    {
        using var handler = StubHandler.Answer(HttpStatusCode.OK, HealthBody);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        await Assert.ThrowsAsync<ArgumentException>(() => client.RequestAsync("GET", "api/v1/health"));
    }

    [Fact]
    public async Task ASlowServerRaisesTimeout()
    {
        using var handler = StubHandler.Hang();
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http, timeout: TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<TimeoutException>(() => client.HealthAsync());
    }

    [Fact]
    public async Task CallerCancellationIsNotATimeout()
    {
        using var handler = StubHandler.Hang();
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.HealthAsync(cancel.Token));
    }

    [Fact]
    public void ARelativeBaseAddressIsRejected()
    {
        using var http = new HttpClient();
        var options = new Pain001ClientOptions { BaseAddress = new Uri("/relative", UriKind.Relative) };

        Assert.Throws<ArgumentException>(() =>
        {
            using var client = new Pain001Client(http, options);
        });
    }

    [Fact]
    public void ANonPositiveTimeoutIsRejected()
    {
        using var http = new HttpClient();
        var options = new Pain001ClientOptions { Timeout = TimeSpan.Zero };

        Assert.Throws<ArgumentException>(() =>
        {
            using var client = new Pain001Client(http, options);
        });
    }

    [Fact]
    public void TheConvenienceConstructorOwnsItsHttpClient()
    {
        using var client = new Pain001Client(new Uri("http://127.0.0.1:8000"), "key");

        // Disposing twice is harmless; the owned HttpClient goes with it.
        client.Dispose();
        client.Dispose();
    }

    [Fact]
    public void TheCallersHttpClientSurvivesDispose()
    {
        using var http = new HttpClient();
        var client = new Pain001Client(http, new Pain001ClientOptions());

        client.Dispose();

        Assert.Equal(TimeSpan.FromSeconds(100), http.Timeout);
        http.Timeout = TimeSpan.FromSeconds(5);
    }

    [Fact]
    public void MessageTypesListsTheThirteenServerTypes()
    {
        Assert.Equal(13, MessageTypes.All.Count);
        Assert.Equal(MessageTypes.All.Count, MessageTypes.All.Distinct().Count());
        Assert.Contains("pain.008.001.08", MessageTypes.All);
    }
}
