// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pain001.Sdk.Tests;

/// <summary>
/// The runner's own semantics, without a server: JSON Pointer, <c>$file</c>
/// resolution and every kind of failure, ported from core's
/// <c>test_the_runner_reports_each_kind_of_failure</c>.
/// </summary>
public sealed class RunnerTests
{
    private static JsonElement Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    [Fact]
    public void ThePointerUnescapesTildeAndSlash()
    {
        JsonElement document = Parse("""{"a/b":{"~":1}}""");

        Assert.True(ConformanceRunner.TryPointer(document, "/a~1b/~0", out JsonElement value));
        Assert.Equal(1, value.GetInt32());
    }

    [Fact]
    public void TheEmptyPointerIsTheWholeDocument()
    {
        JsonElement document = Parse("""[1,2]""");

        Assert.True(ConformanceRunner.TryPointer(document, string.Empty, out JsonElement value));
        Assert.Equal(JsonValueKind.Array, value.ValueKind);
    }

    [Fact]
    public void ThePointerIndexesArraysAndRejectsBadIndexes()
    {
        JsonElement document = Parse("""{"list":[{"name":"x"}]}""");

        Assert.True(ConformanceRunner.TryPointer(document, "/list/0/name", out JsonElement name));
        Assert.Equal("x", name.GetString());
        Assert.False(ConformanceRunner.TryPointer(document, "/list/1", out _));
        Assert.False(ConformanceRunner.TryPointer(document, "/list/-1", out _));
        Assert.False(ConformanceRunner.TryPointer(document, "/list/a", out _));
        Assert.False(ConformanceRunner.TryPointer(document, "/list/99999999999", out _));
        Assert.False(ConformanceRunner.TryPointer(document, "/missing", out _));
        Assert.False(ConformanceRunner.TryPointer(document, "/list/0/name/deeper", out _));
    }

    [Fact]
    public void FileReferencesResolveToTheFixtureText()
    {
        JsonNode body = JsonNode.Parse("""[{"$file":"fixtures/bad-iban.pain.001.001.09.csv"},{"keep":{"$file":"x","other":1}}]""")!;

        JsonNode resolved = ConformanceRunner.Resolve(body)!;

        Assert.StartsWith("id,", resolved[0]!.GetValue<string>(), StringComparison.Ordinal);
        // An object with more keys than $file is not a reference.
        Assert.Equal(1, resolved[1]!["keep"]!["other"]!.GetValue<int>());
    }

    [Fact]
    public void EachKindOfFailureIsReported()
    {
        var response = new Pain001RawResponse(200, Parse("""{"status":"healthy","version":"0.0.86","message":"ok"}"""));
        JsonElement expect = Parse("""
            {"status":200,
             "equals":{"/status":"down","/nope":1},
             "types":{"/status":"integer","/gone":"string"},
             "contains":{"/status":["zzz"],"/version":["0.0."],"/none":["a"]}}
            """);

        List<string> failures = ConformanceRunner.Evaluate(response, expect);

        string[] expected =
        [
            "/status != \"down\"",
            "/nope missing",
            "/status is not integer",
            "/gone missing",
            "/status lacks \"zzz\"",
            "/none missing",
        ];
        Assert.Equal(expected, failures);
    }

    [Fact]
    public void AStatusMismatchStopsTheChecks()
    {
        var response = new Pain001RawResponse(200, Parse("""{"status":"healthy"}"""));

        List<string> failures = ConformanceRunner.Evaluate(response, Parse("""{"status":404,"equals":{"/x":1}}"""));

        Assert.Equal("status 200 != 404", Assert.Single(failures));
    }

    [Fact]
    public void ListDocumentsReportMissingIndexesAndNonStrings()
    {
        var response = new Pain001RawResponse(200, Parse("""[{"name":"berlin_group"},{"name":"iso_2018"}]"""));
        JsonElement expect = Parse("""
            {"status":200,"equals":{"/9/name":"x","/a":1},"contains":{"/0":["x"]}}
            """);

        string[] expected = ["/9/name missing", "/a missing", "/0 is not a string"];
        Assert.Equal(expected, ConformanceRunner.Evaluate(response, expect));
    }

    [Fact]
    public void APassingCaseHasNoFailures()
    {
        var response = new Pain001RawResponse(200, Parse("""{"n":5,"f":1.5,"ok":true,"none":null,"o":{"a":[1,"b"]}}"""));
        JsonElement expect = Parse("""
            {"status":200,
             "equals":{"/n":5.0,"/ok":true,"/none":null,"/o":{"a":[1,"b"]}},
             "types":{"/n":"integer","/f":"number","/ok":"boolean","/none":"null","/o":"object","/o/a":"array","":"object"}}
            """);

        Assert.Empty(ConformanceRunner.Evaluate(response, expect));
    }

    [Fact]
    public void EqualityIsDeepAndStrict()
    {
        Assert.False(ConformanceRunner.JsonEquals(Parse("""{"a":1}"""), Parse("""{"a":1,"b":2}""")));
        Assert.False(ConformanceRunner.JsonEquals(Parse("""{"a":1}"""), Parse("""{"b":1}""")));
        Assert.False(ConformanceRunner.JsonEquals(Parse("""[1,2]"""), Parse("""[1]""")));
        Assert.False(ConformanceRunner.JsonEquals(Parse("""[1,2]"""), Parse("""[1,3]""")));
        Assert.False(ConformanceRunner.JsonEquals(Parse("true"), Parse("false")));
        Assert.False(ConformanceRunner.JsonEquals(Parse("1"), Parse("\"1\"")));
        Assert.True(ConformanceRunner.JsonEquals(Parse("1.5"), Parse("1.50")));
    }

    [Fact]
    public void AFloatIsNotAnInteger()
    {
        Assert.False(ConformanceRunner.IsKind(Parse("5.0"), "integer"));
        Assert.False(ConformanceRunner.IsKind(Parse("5e0"), "integer"));
        Assert.False(ConformanceRunner.IsKind(Parse("\"5\""), "integer"));
        Assert.True(ConformanceRunner.IsKind(Parse("-5"), "integer"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConformanceRunner.IsKind(Parse("5"), "decimal"));
    }
}
