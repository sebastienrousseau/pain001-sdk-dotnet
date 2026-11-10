// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pain001.Sdk;

/// <summary>
/// One item of an HTTP 422 <c>detail</c> array: FastAPI's request
/// validation error. Deserialize <see cref="Pain001ApiException.Detail"/>
/// into a list of these when <see cref="Pain001ApiException.StatusCode"/>
/// is 422. Row errors in a payment file are <see cref="RowError"/>.
/// </summary>
/// <param name="Location">Where the error is: field names (strings) and
/// row or list indexes (integers).</param>
/// <param name="Message">What is wrong.</param>
/// <param name="ErrorType">The error's machine-readable type.</param>
/// <param name="Input">The offending input, when the server reports it.</param>
/// <param name="Context">Extra context, when the server reports it.</param>
public sealed record ValidationError(
    [property: JsonPropertyName("loc")] IReadOnlyList<JsonElement> Location,
    [property: JsonPropertyName("msg")] string Message,
    [property: JsonPropertyName("type")] string ErrorType,
    [property: JsonPropertyName("input")] JsonElement? Input,
    [property: JsonPropertyName("ctx")] JsonElement? Context);
