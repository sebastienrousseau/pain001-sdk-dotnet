// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// Timeouts, cancellation and the opt-in retry policy.
//
// - Pain001ClientOptions.Timeout bounds each request; past it the call
//   raises TimeoutException.
// - A CancellationToken cancels a call; it raises
//   OperationCanceledException, not TimeoutException.
// - Pain001ClientOptions.Retry retries GET calls only (health, faces, a
//   raw GET) on 429, 503 and connection errors. A POST is never retried.
//
// The timeout and cancellation parts talk to a listener on 127.0.0.1
// that accepts the connection and never answers.
//
//   dotnet run --project examples/Timeouts

using System.Net;
using System.Net.Sockets;
using Pain001.Examples;
using Pain001.Sdk;

int failures = 0;

using var http = new HttpClient();
var resilient = new Pain001ClientOptions
{
    BaseAddress = Example.BaseAddress,
    ApiKey = Example.ApiKey,
    Timeout = TimeSpan.FromSeconds(10),
    Retry = new RetryPolicy { MaxAttempts = 4, InitialDelay = TimeSpan.FromMilliseconds(100) },
};
using (var client = new Pain001Client(http, resilient))
{
    HealthResponse health = await client.HealthAsync().ConfigureAwait(false);
    Console.WriteLine($"with a 10 s timeout and up to 4 attempts: {health.Status}");
}

using var silent = new TcpListener(IPAddress.Loopback, 0);
silent.Start();
var silentAddress = new Uri($"http://127.0.0.1:{((IPEndPoint)silent.LocalEndpoint).Port}");

var impatient = new Pain001ClientOptions { BaseAddress = silentAddress, Timeout = TimeSpan.FromMilliseconds(300) };
using (var client = new Pain001Client(http, impatient))
{
    try
    {
        await client.HealthAsync().ConfigureAwait(false);
        failures += Example.Fail("the silent server answered");
    }
    catch (TimeoutException error)
    {
        Console.WriteLine($"300 ms timeout: {error.Message}");
    }
}

using (var client = new Pain001Client(http, new Pain001ClientOptions { BaseAddress = silentAddress }))
{
    using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
    try
    {
        await client.HealthAsync(cancel.Token).ConfigureAwait(false);
        failures += Example.Fail("the silent server answered");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("cancelled after 200 ms: OperationCanceledException");
    }
}

return failures;
