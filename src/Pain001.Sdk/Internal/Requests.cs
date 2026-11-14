// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json.Serialization;

namespace Pain001.Sdk.Internal;

/// <summary>The body of the validate and generate calls (<c>UiFilePayload</c>).</summary>
internal sealed record UiFilePayload(
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("message_type")] string MessageType,
    [property: JsonPropertyName("scheme")] string? Scheme);

/// <summary>The body of the check call (<c>CheckRequest</c>).</summary>
internal sealed record CheckRequest(
    [property: JsonPropertyName("xml")] string Xml,
    [property: JsonPropertyName("scheme")] string? Scheme,
    [property: JsonPropertyName("schematron")] string? Schematron);

/// <summary>The body of the face projection call (<c>FaceRequest</c>).</summary>
internal sealed record FaceRequest(
    [property: JsonPropertyName("xml")] string Xml,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("bindings")] IReadOnlyDictionary<string, object?>? Bindings);
