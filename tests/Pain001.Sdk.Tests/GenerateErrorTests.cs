// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Net;
using System.Text.Json;

namespace Pain001.Sdk.Tests;

/// <summary>
/// Generate's failure modes against a stub handler: refusals (HTTP 422,
/// <c>success: false</c>), answers without XML, and the exception types'
/// standard constructors.
/// </summary>
public sealed class GenerateErrorTests
{
    [Fact]
    public async Task AGenerateRefusalCarriesTheRowFindings()
    {
        const string Answer = """
            {"success":false,"message":"Validation failed: 1 row(s) with errors","filename":null,"xml":null,
             "validation_errors":[{"field":"$.currency","message":"not a valid currency","value":"EURO"}],
             "scheme_violations":[]}
            """;
        using var handler = StubHandler.Answer(HttpStatusCode.UnprocessableEntity, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAsync<Pain001GenerationRefusedException>(
            () => client.GenerateAsync("x", "p.csv", MessageTypes.Pain001V09));

        Assert.Equal(422, error.StatusCode);
        Assert.False(error.Response.Success);
        Assert.Null(error.Response.Xml);
        RowError row = Assert.Single(error.Response.ValidationErrors!);
        Assert.Equal("$.currency", row.Field);
        Assert.Equal("EURO", row.Value!.Value.GetString());
        Assert.Empty(error.Response.SchemeViolations!);
        Assert.False(error.Detail.GetProperty("success").GetBoolean());
        Assert.Equal(
            "The pain001 API refused to generate the XML (HTTP 422): 1 validation error, 0 scheme violations; see Response for the findings.",
            error.Message);
        // The rejected value stays out of the message, which ends up in logs.
        Assert.DoesNotContain("EURO", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AGenerateRefusalCarriesTheSchemeViolations()
    {
        const string Answer = """
            {"success":false,"message":"Scheme rules failed","filename":null,"xml":null,"validation_errors":[],
             "scheme_violations":[{"rule":"UK-CHAPS-PURP","severity":"error","field":"purpose_code"},
                                  {"rule":"UK-CHAPS-X","severity":"error","field":"x"}]}
            """;
        using var handler = StubHandler.Answer(HttpStatusCode.UnprocessableEntity, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        // A caller that catches the base exception still sees the refusal.
        var error = await Assert.ThrowsAnyAsync<Pain001ApiException>(
            () => client.GenerateAsync("x", "p.csv", MessageTypes.Pain001V09, scheme: "uk-chaps"));

        var refused = Assert.IsType<Pain001GenerationRefusedException>(error);
        Assert.Equal(422, refused.StatusCode);
        Assert.Empty(refused.Response.ValidationErrors!);
        Assert.Equal("UK-CHAPS-PURP", refused.Response.SchemeViolations![0].GetProperty("rule").GetString());
        Assert.Contains("0 validation errors, 2 scheme violations", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"success":false,"message":"no","xml":null}""")]
    [InlineData("""{"success":false,"message":"no","validation_errors":"not a list"}""")]
    [InlineData("""{"success":true,"message":"odd","xml":null}""")]
    [InlineData("""{"detail":[{"loc":["body"],"msg":"bad","type":"enum"}]}""")]
    [InlineData("""[1,2]""")]
    public async Task OnlyASuccessFalseGenerateBodyIsARefusal(string answer)
    {
        using var handler = StubHandler.Answer(HttpStatusCode.UnprocessableEntity, answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAnyAsync<Pain001ApiException>(
            () => client.GenerateAsync("x", "p.csv", MessageTypes.Pain001V09));

        bool refusal = answer.Contains("\"success\":false", StringComparison.Ordinal)
            && !answer.Contains("not a list", StringComparison.Ordinal);
        Assert.Equal(refusal, error is Pain001GenerationRefusedException);
        Assert.Equal(422, error.StatusCode);
    }

    [Fact]
    public async Task ARefusalShapedBodyFromAnotherStatusOrMethodIsNotARefusal()
    {
        const string Answer = """{"success":false,"message":"no","xml":null}""";
        using var handler = StubHandler.Answer(HttpStatusCode.BadRequest, Answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);
        using var handler422 = StubHandler.Answer(HttpStatusCode.UnprocessableEntity, Answer);
        using var http422 = new HttpClient(handler422);
        using Pain001Client client422 = StubHandler.Client(http422);

        var fromGenerate = await Assert.ThrowsAsync<Pain001ApiException>(
            () => client.GenerateAsync("x", "p.csv", MessageTypes.Pain001V09));
        var fromCheck = await Assert.ThrowsAsync<Pain001ApiException>(() => client422.CheckAsync("<x/>"));

        Assert.Equal(400, fromGenerate.StatusCode);
        Assert.Equal(422, fromCheck.StatusCode);
    }

    [Fact]
    public void TheApiExceptionHasTheStandardConstructors()
    {
        var plain = new Pain001ApiException();
        Assert.Equal("The pain001 API returned an error.", plain.Message);
        Assert.Equal(0, plain.StatusCode);
        Assert.Equal(JsonValueKind.Undefined, plain.Detail.ValueKind);
        Assert.Equal("m", new Pain001ApiException("m").Message);
        var inner = new InvalidOperationException("cause");
        Assert.Same(inner, new Pain001ApiException("m", inner).InnerException);
    }

    [Fact]
    public void TheRefusalHasTheStandardConstructors()
    {
        Assert.Equal("The pain001 API refused to generate the XML.", new Pain001GenerationRefusedException().Message);
        var withMessage = new Pain001GenerationRefusedException("m");
        Assert.Equal("m", withMessage.Message);
        Assert.False(withMessage.Response.Success);
        var inner = new InvalidOperationException("cause");
        var withCause = new Pain001GenerationRefusedException("m", inner);
        Assert.Same(inner, withCause.InnerException);
        Assert.Equal("m", withCause.Response.Message);
    }

    [Theory]
    [InlineData("""{"success":false,"message":"Validation failed","xml":null,"validation_errors":[],"scheme_violations":[]}""")]
    [InlineData("""{"success":true,"message":"done","xml":null}""")]
    [InlineData("""{"success":true,"message":"done","xml":""}""")]
    [InlineData("""{"success":true,"message":"done"}""")]
    [InlineData("""{"success":false,"message":"odd","xml":"<Document/>"}""")]
    public async Task ASuccessfulGenerateWithoutTheXmlRaisesNoDocument(string answer)
    {
        using var handler = StubHandler.Answer(HttpStatusCode.OK, answer);
        using var http = new HttpClient(handler);
        using Pain001Client client = StubHandler.Client(http);

        var error = await Assert.ThrowsAsync<Pain001NoDocumentException>(
            () => client.GenerateAsync("x", "p.csv", MessageTypes.Pain001V09));

        Assert.Equal(200, error.StatusCode);
        Assert.NotNull(error.Response);
        Assert.Equal(
            "The pain001 API answered generate with HTTP 200 but no XML document; see Response.",
            error.Message);
    }

    [Fact]
    public void NoDocumentHasTheStandardConstructors()
    {
        var plain = new Pain001NoDocumentException();
        Assert.Equal("The pain001 API answered generate without an XML document.", plain.Message);
        Assert.Null(plain.Response);
        Assert.Equal(0, plain.StatusCode);
        Assert.Equal("m", new Pain001NoDocumentException("m").Message);
        var inner = new InvalidOperationException("cause");
        Assert.Same(inner, new Pain001NoDocumentException("m", inner).InnerException);
        Assert.Throws<ArgumentNullException>(() => new Pain001NoDocumentException(200, null!));
    }
}
