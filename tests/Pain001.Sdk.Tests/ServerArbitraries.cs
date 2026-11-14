// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Globalization;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.Fluent;

namespace Pain001.Sdk.Tests;

/// <summary>Any answer a server, or a proxy in front of it, could send.</summary>
/// <param name="Status">The HTTP status code.</param>
/// <param name="Body">The exact response bytes.</param>
/// <param name="MediaType">The response's Content-Type media type.</param>
public sealed record ServerAnswer(int Status, ReadOnlyMemory<byte> Body, string MediaType)
{
    /// <inheritdoc/>
    public override string ToString() =>
        $"HTTP {Status} {MediaType}: {Convert.ToHexString(Body.Span)}";
}

/// <summary>How an error response carries its detail.</summary>
public enum DetailShape
{
    /// <summary><c>{"detail": "..."}</c>, as most pain001 errors.</summary>
    Text,

    /// <summary>FastAPI's HTTP 422 <c>{"detail": [{...}]}</c>.</summary>
    ValidationArray,

    /// <summary>A JSON body without a <c>detail</c> field.</summary>
    WholeBody,

    /// <summary>A body that is not JSON, such as a proxy's error page.</summary>
    PlainText,
}

/// <summary>An error response whose detail echoes payment data.</summary>
/// <param name="Status">An HTTP status outside 2xx.</param>
/// <param name="Secret">Text that must never reach the exception message.</param>
/// <param name="Shape">Where the body puts the secret.</param>
public sealed record ErrorAnswer(int Status, string Secret, DetailShape Shape);

/// <summary>A well-formed response for one typed method.</summary>
/// <param name="Method">The typed method that reads it.</param>
/// <param name="Json">The response body.</param>
public sealed record TypedAnswer(string Method, string Json);

/// <summary>FsCheck generators for what a pain001 server can answer.</summary>
public static class ServerArbitraries
{
    // Field names of the response models, so random objects sometimes
    // look like a typed answer with the wrong value types.
    private static readonly string[] Keys =
    [
        "status", "version", "message", "detail", "is_valid", "total_rows", "valid_rows",
        "invalid_rows", "errors", "scheme_violations", "success", "filename", "xml",
        "validation_errors", "message_type", "passed", "warnings", "rungs", "findings",
        "name", "description", "bindings", "face", "payload", "loss_report", "loss_ratio",
        "field", "value", "loc", "msg", "type", "input", "ctx",
    ];

    private static readonly string[] MediaTypes =
    [
        "application/json", "application/problem+json", "text/html", "text/plain",
        "application/octet-stream",
    ];

    // Raw JSON scalars the generator would rarely reach on its own: number
    // edges, escapes and a lone surrogate (which Python's json emits).
    private static readonly string[] OddScalars =
    [
        "0", "-0", "1e400", "-1e400", "2147483648", "9223372036854775808", "1.5", "1E2",
        "\"\\ud800\"", "\"\\udc00x\"", "\"\\u0000\"", "\"\"", "\"DE89 3704 0044 0532 0130 00\"",
    ];

    /// <summary>Any status, any bytes, any media type.</summary>
    public static Arbitrary<ServerAnswer> Answer() =>
        (from status in Status()
         from body in Body()
         from mediaType in Gen.Elements(MediaTypes)
         select new ServerAnswer(status, body, mediaType)).ToArbitrary();

    /// <summary>An error status with a secret in one of the detail shapes.</summary>
    public static Arbitrary<ErrorAnswer> Error() =>
        (from status in Gen.OneOf(Gen.Choose(400, 599), Gen.Choose(100, 199), Gen.Choose(300, 399))
         from secret in Secret()
         from shape in Gen.Elements(Enum.GetValues<DetailShape>())
         select new ErrorAnswer(status, secret, shape)).ToArbitrary();

    /// <summary>A well-formed answer for one of the typed methods.</summary>
    public static Arbitrary<TypedAnswer> Typed() =>
        Gen.OneOf(
            Health().Select(v => new TypedAnswer("health", Serialize(v))),
            Validation().Select(v => new TypedAnswer("validate", Serialize(v))),
            Check().Select(v => new TypedAnswer("check", Serialize(v))),
            Faces().Select(v => new TypedAnswer("faces", Serialize(v)))).ToArbitrary();

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);

    private static Gen<int> Status() =>
        Gen.OneOf(
            Gen.Choose(200, 299),
            Gen.Choose(400, 599),
            Gen.Choose(100, 599),
            Gen.Elements(200, 201, 204, 400, 401, 404, 422, 500, 502, 503));

    private static Gen<ReadOnlyMemory<byte>> Body() =>
        Gen.OneOf(
            Json().Select(Utf8),
            Json().SelectMany(Mutate),
            Text().Select(Utf8),
            Bytes(),
            Gen.Constant(ReadOnlyMemory<byte>.Empty));

    private static ReadOnlyMemory<byte> Utf8(string text) => Encoding.UTF8.GetBytes(text);

    private static Gen<ReadOnlyMemory<byte>> Bytes() =>
        Gen.Choose(0, 255).Select(b => (byte)b).ArrayOf().Select(b => (ReadOnlyMemory<byte>)b);

    // Valid JSON damaged one way: cut short, one byte replaced, a byte
    // that is never valid UTF-8 inserted, or a byte order mark in front.
    private static Gen<ReadOnlyMemory<byte>> Mutate(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        return from at in Gen.Choose(0, bytes.Length)
               from value in Gen.Choose(0, 255)
               from how in Gen.Choose(0, 3)
               select Damage(bytes, at, (byte)value, how);
    }

    private static ReadOnlyMemory<byte> Damage(byte[] bytes, int at, byte value, int how) => how switch
    {
        0 => bytes.AsMemory(0, at),
        1 when at < bytes.Length => Replace(bytes, at, value),
        2 => (byte[])[.. bytes.AsSpan(0, at), 0xFF, .. bytes.AsSpan(at)],
        _ => (byte[])[0xEF, 0xBB, 0xBF, .. bytes],
    };

    private static byte[] Replace(byte[] bytes, int at, byte value)
    {
        byte[] copy = (byte[])bytes.Clone();
        copy[at] = value;
        return copy;
    }

    private static Gen<string> Json() => Gen.Sized(size => JsonValue(Math.Min(size, 24)));

    private static Gen<string> JsonValue(int size) =>
        size <= 1 ? Scalar() : Gen.OneOf(Scalar(), JsonArray(size), JsonObject(size));

    private static Gen<string> JsonArray(int size) =>
        from count in Gen.Choose(0, 4)
        from items in JsonValue(size / 2).ListOf(count)
        select "[" + string.Join(",", items) + "]";

    private static Gen<string> JsonObject(int size) =>
        from count in Gen.Choose(0, 5)
        from members in Gen.Zip(Key(), JsonValue(size / 2)).ListOf(count)
        select "{" + string.Join(",", members.Select(m => Quote(m.Item1) + ":" + m.Item2)) + "}";

    private static Gen<string> Key() => Gen.OneOf(Gen.Elements(Keys), Text());

    private static Gen<string> Scalar() =>
        Gen.OneOf(
            Gen.Elements("null", "true", "false"),
            Gen.Choose(-1000, 1000).Select(i => i.ToString(CultureInfo.InvariantCulture)),
            Gen.Elements(OddScalars),
            Text().Select(Quote));

    private static string Quote(string text) => JsonSerializer.Serialize(text);

    // Any UTF-16 code unit, lone surrogates included.
    private static Gen<string> Text() =>
        Gen.Choose(0, 0xFFFF).Select(c => (char)c).ArrayOf().Select(chars => new string(chars));

    private static Gen<string> Secret() =>
        from digits in Gen.Choose(0, 99_999_999)
        from tail in Text()
        select string.Create(CultureInfo.InvariantCulture, $"DE89 3704 {digits:D8} {tail}");

    private static Gen<string> Word() =>
        Gen.Choose('a', 'z').Select(c => (char)c).ListOf().Select(chars => string.Concat(chars));

    private static Gen<HealthResponse> Health() =>
        from status in Word()
        from version in Word()
        from message in Text()
        select new HealthResponse(status, version, message);

    private static Gen<RowError> RowError() =>
        from field in Word()
        from message in Text()
        select new RowError(field, message, null);

    private static Gen<ValidationResponse> Validation() =>
        from total in Gen.Choose(0, 100_000)
        from invalid in Gen.Choose(0, total)
        from errors in RowError().ListOf()
        select new ValidationResponse(invalid == 0, total, total - invalid, invalid, errors, null);

    private static Gen<CheckFinding> Finding() =>
        from rung in Gen.Elements("xsd", "mdr", "scheme", "schematron")
        from rule in Word()
        from severity in Gen.Elements("error", "warning")
        from message in Text()
        from location in Gen.OneOf(Gen.Constant<string?>(null), Text().Select(t => (string?)t))
        select new CheckFinding(rung, rule, severity, message, location);

    private static Gen<CheckResponse> Check() =>
        from type in Gen.Elements(MessageTypes.All.ToArray())
        from findings in Finding().ListOf()
        from rungs in Word().ListOf()
        select new CheckResponse(
            type,
            findings.Count == 0,
            findings.Count(f => f.Severity == "error"),
            findings.Count(f => f.Severity == "warning"),
            rungs,
            findings);

    private static Gen<IReadOnlyList<FaceInfo>> Faces() =>
        (from name in Word()
         from description in Text()
         from bindings in Word().ListOf()
         select new FaceInfo(name, description, bindings)).ListOf().Select(l => (IReadOnlyList<FaceInfo>)l);
}
