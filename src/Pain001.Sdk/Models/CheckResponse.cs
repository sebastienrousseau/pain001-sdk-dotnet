// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json.Serialization;

namespace Pain001.Sdk;

/// <summary>One problem one rung of the validation ladder found.</summary>
/// <param name="Rung">The rung: <c>xsd</c>, <c>mdr</c>, <c>scheme</c> or
/// <c>schematron</c>.</param>
/// <param name="Rule">The rule that fired.</param>
/// <param name="Severity"><c>error</c> or <c>warning</c>.</param>
/// <param name="Message">What is wrong.</param>
/// <param name="Location">The element path, context or transaction, when known.</param>
public sealed record CheckFinding(
    [property: JsonPropertyName("rung")] string Rung,
    [property: JsonPropertyName("rule")] string Rule,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("location")] string? Location);

/// <summary>The answer to <c>POST /api/v1/check</c>.</summary>
/// <param name="MessageType">The detected message type.</param>
/// <param name="Passed">Whether no rung found an error.</param>
/// <param name="Errors">How many errors were found.</param>
/// <param name="Warnings">How many warnings were found.</param>
/// <param name="Rungs">The rungs that ran, in order.</param>
/// <param name="Findings">Every finding.</param>
public sealed record CheckResponse(
    [property: JsonPropertyName("message_type")] string MessageType,
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("errors")] int Errors,
    [property: JsonPropertyName("warnings")] int Warnings,
    [property: JsonPropertyName("rungs")] IReadOnlyList<string> Rungs,
    [property: JsonPropertyName("findings")] IReadOnlyList<CheckFinding> Findings);
