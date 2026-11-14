// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json.Serialization;

namespace Pain001.Sdk;

/// <summary>The answer to <c>GET /api/v1/health</c>.</summary>
/// <param name="Status">The service status, <c>healthy</c> when up.</param>
/// <param name="Version">The server's pain001 version.</param>
/// <param name="Message">A human-readable status message.</param>
public sealed record HealthResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("message")] string Message);
