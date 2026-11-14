// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// The API key: a server started with PAIN001_API_KEY set answers 401
// unless the request carries `Authorization: Bearer <key>`. The client
// sends Pain001ClientOptions.ApiKey that way, to the configured server
// only. Read the key from the environment or a secret store, never from
// source code.
//
// This example sends PAIN001_API_KEY, then a wrong key. Against a server
// without a key (loopback development) both succeed; against one with a
// key, the wrong one is refused with HTTP 401.
//
//   PAIN001_API_KEY=... dotnet run --project examples/ApiKey

using Pain001.Examples;
using Pain001.Sdk;

string csv = Example.Fixture("credit-transfer.pain.001.001.09.csv");
using var http = new HttpClient();

var withKey = new Pain001ClientOptions { BaseAddress = Example.BaseAddress, ApiKey = Example.ApiKey };
using (var client = new Pain001Client(http, withKey))
{
    ValidationResponse result = await client.ValidateAsync(csv, "payments.csv", MessageTypes.Pain001V09).ConfigureAwait(false);
    string sent = Example.ApiKey is null ? "no key" : "the key from PAIN001_API_KEY";
    Console.WriteLine($"with {sent}: valid={result.IsValid}");
}

var wrongKey = new Pain001ClientOptions { BaseAddress = Example.BaseAddress, ApiKey = "not-the-key" };
using (var client = new Pain001Client(http, wrongKey))
{
    try
    {
        await client.ValidateAsync(csv, "payments.csv", MessageTypes.Pain001V09).ConfigureAwait(false);
        Console.WriteLine("with a wrong key: accepted, so this server has no PAIN001_API_KEY set");
        return Example.ApiKey is null ? 0 : Example.Fail("a key was configured but a wrong one was accepted");
    }
    catch (Pain001ApiException error) when (error.StatusCode == 401)
    {
        Console.WriteLine($"with a wrong key: refused, {error.Message}");
    }
}

return 0;
