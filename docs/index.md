<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# pain001-sdk-dotnet

A typed .NET client for the pain001 REST API (`pain001 serve`):
validate, generate and check ISO 20022 pain.001 and pain.008 payment
files from C#, F# or any .NET language. The server does the work; this
package turns its JSON into typed async calls, and core's conformance
kit proves it against a live server in CI.

```csharp
using Pain001.Sdk;

using var client = new Pain001Client(new Uri("http://127.0.0.1:8000"));
string csv = await File.ReadAllTextAsync("payments.csv");
UiGenerateResponse result = await client.GenerateAsync(csv, "payments.csv", MessageTypes.Pain001V09);
await File.WriteAllTextAsync("payments.xml", result.Xml);
```

## In this manual

- [Getting started](getting-started.md): install, a server, the first call.
- [Examples](examples.md): one runnable project per method.
- [API reference](api/index.md): every public type and member, from the
  XML documentation comments.
- [Architecture](ARCHITECTURE.md) and the
  [decision records](adr/index.md).
- [Comparison](COMPARISON.md) with the suite's other SDKs.
- [Benchmarks](BENCHMARKS.md): the client's cost and loopback round trips.
- [Packaging](packaging.md), [Roadmap](ROADMAP.md).
- For contributors: [Development](development.md),
  [Contributing](contributing.md), [Governance](governance.md),
  [Security](security.md), [Support](support.md),
  [Changelog](changelog.md).
