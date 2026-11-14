// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    internal static JsonElement ParseBody(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return default;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(bytes);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(Encoding.UTF8.GetString(bytes));
        }
    }

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

        return response.Body.Deserialize<T>(Options)
            ?? throw new JsonException("The pain001 API returned an empty body where JSON was expected.");
    }
}
