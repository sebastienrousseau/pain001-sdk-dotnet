// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;

namespace Pain001.Sdk;

/// <summary>
/// The status and parsed body of a call made with
/// <see cref="Pain001Client.RequestAsync"/>.
/// </summary>
/// <param name="StatusCode">The HTTP status code.</param>
/// <param name="Body">The parsed JSON body. A body that is not JSON is
/// returned as a JSON string holding its text; an empty body is
/// <see cref="JsonValueKind.Undefined"/>.</param>
public sealed record Pain001RawResponse(int StatusCode, JsonElement Body);
