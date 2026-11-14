<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Getting started

## 1. Run a server

The SDK calls `pain001 serve`, the REST API of the Python package
`pain001`. Install it with its `api` extra and start it on loopback:

```sh
pip install 'pain001[api]==0.0.87'
pain001 serve --host 127.0.0.1 --port 8000
```

`GET http://127.0.0.1:8000/api/v1/health` answers
`{"status":"healthy",...}` once it is up. For any server that is not on
loopback, set `PAIN001_API_KEY` on the server and pass the same key to
the client.

## 2. Add the package

```sh
dotnet add package Pain001.Sdk --version 0.0.88
```

Until the first release is on NuGet, reference the project from a clone
instead:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/pain001-sdk-dotnet/src/Pain001.Sdk/Pain001.Sdk.csproj" />
</ItemGroup>
```

The package targets `net10.0` and has no dependency of its own.

## 3. Make a call

```csharp
using Pain001.Sdk;

using var client = new Pain001Client(new Uri("http://127.0.0.1:8000"));

HealthResponse health = await client.HealthAsync();
Console.WriteLine($"pain001 {health.Version}: {health.Status}");

string csv = await File.ReadAllTextAsync("payments.csv");
try
{
    UiGenerateResponse result = await client.GenerateAsync(csv, "payments.csv", MessageTypes.Pain001V09);
    await File.WriteAllTextAsync("payments.xml", result.Xml);
}
catch (Pain001GenerationRefusedException refused)
{
    // The server refused the data; the findings are in refused.Response.
    Console.Error.WriteLine(refused.Message);
}
```

## 4. Configure it for production

```csharp
var options = new Pain001ClientOptions
{
    BaseAddress = new Uri("https://pain001.internal.example"),
    ApiKey = Environment.GetEnvironmentVariable("PAIN001_API_KEY"),
    Timeout = TimeSpan.FromSeconds(10),
    Retry = new RetryPolicy(), // GET calls only; off when left null
};
using var client = new Pain001Client(httpClientFromYourFactory, options);
```

| Option | Default | Meaning |
| :--- | :--- | :--- |
| `BaseAddress` | `http://127.0.0.1:8000` | The server's absolute address; a path prefix is kept |
| `ApiKey` | none | Sent as `Authorization: Bearer <key>` |
| `Timeout` | 30 seconds | Per request; past it the call raises `TimeoutException` |
| `Retry` | none | A `RetryPolicy` for `GET` calls; a `POST` is never retried |
| `TimeProvider` | `TimeProvider.System` | The clock for retry waits |

The client never changes an `HttpClient` you pass in and does not
dispose it. Errors are described in
[ADR 0003](adr/0003-error-model.md), retries in
[ADR 0004](adr/0004-opt-in-retries-for-get-only.md).
