// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// RequestAsync: any method and path, returning the status and the
// parsed JSON body whatever the status; it never throws for one. Use it
// for an endpoint this SDK has no typed method for yet.
//
//   dotnet run --project examples/RawRequest

using System.Text.Json.Nodes;
using Pain001.Examples;
using Pain001.Sdk;

using Pain001Client client = Example.CreateClient();

Pain001RawResponse health = await client.RequestAsync("GET", "/api/v1/health").ConfigureAwait(false);
Console.WriteLine($"GET /api/v1/health: HTTP {health.StatusCode}, status={health.Body.GetProperty("status").GetString()}");

var body = new JsonObject
{
    ["filename"] = "payments.csv",
    ["content"] = Example.Fixture("credit-transfer.pain.001.001.09.csv"),
    ["message_type"] = MessageTypes.Pain001V09,
};
Pain001RawResponse validate = await client.RequestAsync("POST", "/api/v1/ui/validate", body).ConfigureAwait(false);
Console.WriteLine($"POST /api/v1/ui/validate: HTTP {validate.StatusCode}, is_valid={validate.Body.GetProperty("is_valid").GetBoolean()}");

Pain001RawResponse missing = await client.RequestAsync("GET", "/api/v1/no-such-endpoint").ConfigureAwait(false);
Console.WriteLine($"GET /api/v1/no-such-endpoint: HTTP {missing.StatusCode}, returned rather than thrown");

return health.StatusCode == 200 && validate.StatusCode == 200 && missing.StatusCode == 404
    ? 0
    : Example.Fail("unexpected status");
