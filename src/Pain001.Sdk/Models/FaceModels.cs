// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pain001.Sdk;

/// <summary>One face: a target format a pain.001 document can be projected into.</summary>
/// <param name="Name">The face name, used in the request path.</param>
/// <param name="Description">The target standard.</param>
/// <param name="Bindings">The binding keys the face accepts; any other is refused.</param>
public sealed record FaceInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("bindings")] IReadOnlyList<string> Bindings);

/// <summary>What a projection carried and what it did not, by indexed path.</summary>
/// <param name="Face">The face name.</param>
/// <param name="TotalSourceElements">How many source leaves the document had.</param>
/// <param name="MappedCount">How many were carried.</param>
/// <param name="UnmappedCount">How many were not.</param>
/// <param name="IsLossless">Whether every leaf was carried.</param>
/// <param name="LossRatio">The share of source leaves not carried.</param>
/// <param name="MappedPaths">The carried paths.</param>
/// <param name="UnmappedPaths">The paths not carried.</param>
public sealed record LossReport(
    [property: JsonPropertyName("face")] string Face,
    [property: JsonPropertyName("total_source_elements")] int TotalSourceElements,
    [property: JsonPropertyName("mapped_count")] int MappedCount,
    [property: JsonPropertyName("unmapped_count")] int UnmappedCount,
    [property: JsonPropertyName("is_lossless")] bool IsLossless,
    [property: JsonPropertyName("loss_ratio")] double LossRatio,
    [property: JsonPropertyName("mapped_paths")] IReadOnlyList<string> MappedPaths,
    [property: JsonPropertyName("unmapped_paths")] IReadOnlyList<string> UnmappedPaths);

/// <summary>The answer to <c>POST /api/v1/faces/{face}</c>.</summary>
/// <param name="Face">The face name.</param>
/// <param name="Version">The source pain.001 edition.</param>
/// <param name="Payload">The body in the target's shape, as raw JSON.</param>
/// <param name="LossReport">What the projection did not carry.</param>
public sealed record FaceResponse(
    [property: JsonPropertyName("face")] string Face,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("payload")] JsonElement Payload,
    [property: JsonPropertyName("loss_report")] LossReport LossReport);
