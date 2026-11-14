// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pain001.Sdk;

/// <summary>
/// One row-level error in a payment file, in pain001's <c>RowError</c>
/// shape. Not to be confused with <see cref="ValidationError"/>, the
/// shape of a request rejected with HTTP 422.
/// </summary>
/// <param name="Field">The column or field at fault.</param>
/// <param name="Message">What is wrong.</param>
/// <param name="Value">The offending value, or <see langword="null"/>
/// when the server does not report one.</param>
public sealed record RowError(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("value")] JsonElement? Value);
