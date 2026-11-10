// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pain001.Sdk;

/// <summary>The answer to <c>POST /api/v1/ui/validate</c>.</summary>
/// <param name="IsValid">Whether every row is valid.</param>
/// <param name="TotalRows">How many rows the file holds.</param>
/// <param name="ValidRows">How many rows passed.</param>
/// <param name="InvalidRows">How many rows failed.</param>
/// <param name="Errors">The row-level errors, if any.</param>
/// <param name="SchemeViolations">Scheme-rulebook violations, when a
/// scheme was given; their shape depends on the rulebook.</param>
public sealed record ValidationResponse(
    [property: JsonPropertyName("is_valid")] bool IsValid,
    [property: JsonPropertyName("total_rows")] int TotalRows,
    [property: JsonPropertyName("valid_rows")] int ValidRows,
    [property: JsonPropertyName("invalid_rows")] int InvalidRows,
    [property: JsonPropertyName("errors")] IReadOnlyList<RowError>? Errors,
    [property: JsonPropertyName("scheme_violations")] IReadOnlyList<JsonElement>? SchemeViolations);
