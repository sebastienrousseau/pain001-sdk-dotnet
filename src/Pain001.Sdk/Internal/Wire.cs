// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Pain001.Sdk.Internal;

/// <summary>JSON encoding and decoding shared by every call.</summary>
internal static class Wire
{
    /// <summary>
    /// Omits optional request fields left <see langword="null"/>, so the
    /// server applies its own defaults.
    /// </summary>
    internal static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Parses a response body. An empty body is
    /// <see cref="JsonValueKind.Undefined"/>; a body that is not JSON (a
    /// proxy's HTML error page) becomes a JSON string holding its text.
    /// </summary>
    /// <remarks>
    /// JSON text is UTF-8 (RFC 8259, section 8.1), but
    /// <see cref="JsonDocument.Parse(ReadOnlyMemory{byte}, JsonDocumentOptions)"/>
    /// accepts invalid UTF-8 inside a string and the element it returns
    /// throws <see cref="InvalidOperationException"/> when read. Such a
    /// body is not JSON, so it is kept as text, with each invalid byte
    /// replaced by U+FFFD.
    /// </remarks>
    internal static JsonElement ParseBody(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return default;
        }

        if (!Utf8.IsValid(bytes))
        {
            return AsText(bytes);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(bytes);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return AsText(bytes);
        }
    }

    private static JsonElement AsText(byte[] bytes) =>
        JsonSerializer.SerializeToElement(Encoding.UTF8.GetString(bytes));

    /// <summary>
    /// The error detail: the body's <c>detail</c> field when it has one,
    /// otherwise the whole body.
    /// </summary>
    internal static JsonElement ExtractDetail(JsonElement body)
    {
        if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("detail", out JsonElement detail))
        {
            return detail;
        }

        return body;
    }

    /// <summary>
    /// The refusal a generate call answers with since pain001 0.0.87:
    /// HTTP 422 whose body is a generate response with <c>success: false</c>.
    /// Any other answer, including a 422 body that does not decode as a
    /// generate response, is <see langword="null"/>.
    /// </summary>
    internal static Pain001GenerationRefusedException? AsRefusal(Pain001RawResponse response)
    {
        JsonElement body = response.Body;
        if (response.StatusCode != 422
            || body.ValueKind != JsonValueKind.Object
            || !body.TryGetProperty("success", out JsonElement success)
            || success.ValueKind != JsonValueKind.False)
        {
            return null;
        }

        try
        {
            UiGenerateResponse decoded = body.Deserialize<UiGenerateResponse>(Options)!;
            return new Pain001GenerationRefusedException(response.StatusCode, body, decoded);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Whether a status code is in the 2xx range.</summary>
    internal static bool IsSuccess(int statusCode) => statusCode is >= 200 and <= 299;

    /// <summary>Decodes a successful response into its typed shape.</summary>
    internal static T Decode<T>(Pain001RawResponse response)
    {
        if (!IsSuccess(response.StatusCode))
        {
            throw new Pain001ApiException(response.StatusCode, ExtractDetail(response.Body));
        }

        if (response.Body.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new JsonException("The pain001 API returned an empty body where JSON was expected.");
        }

        // Only JSON null deserializes to null, and it was refused above.
        return response.Body.Deserialize<T>(Options)!;
    }
}
