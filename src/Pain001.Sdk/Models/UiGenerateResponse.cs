// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pain001.Sdk;

/// <summary>The answer to <c>POST /api/v1/ui/generate</c>.</summary>
/// <param name="Success">Whether the XML was generated.</param>
/// <param name="Message">The result message.</param>
/// <param name="Filename">A suggested name for the XML file.</param>
/// <param name="Xml">The generated XML document, on success.</param>
/// <param name="ValidationErrors">The blocking row-level errors, on failure.</param>
/// <param name="SchemeViolations">The blocking scheme-rulebook
/// violations, on failure.</param>
public sealed record UiGenerateResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("filename")] string? Filename,
    [property: JsonPropertyName("xml")] string? Xml,
    [property: JsonPropertyName("validation_errors")] IReadOnlyList<RowError>? ValidationErrors,
    [property: JsonPropertyName("scheme_violations")] IReadOnlyList<JsonElement>? SchemeViolations);
