<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

<p align="center">
  <img src="https://cloudcdn.pro/pain001/v1/logos/pain001.svg" alt="pain001-sdk-dotnet logo" width="128" />
</p>

<h1 align="center">pain001-sdk-dotnet</h1>

<p align="center">
  A typed .NET client for the pain001 REST API: validate, generate and check ISO 20022 pain.001 and pain.008 payment files from C#.
</p>

<p align="center">
  <a href="https://github.com/sebastienrousseau/pain001-sdk-dotnet/actions"><img src="https://img.shields.io/github/actions/workflow/status/sebastienrousseau/pain001-sdk-dotnet/ci.yml?style=for-the-badge&logo=github" alt="Build" height="28" /></a>
  <a href="https://www.nuget.org/packages/Pain001.Sdk"><img src="https://img.shields.io/nuget/v/Pain001.Sdk?style=for-the-badge&color=fc8d62&logo=nuget" alt="Registry" height="28" /></a>
  <a href="https://sebastienrousseau.github.io/pain001-sdk-dotnet/api/"><img src="https://img.shields.io/badge/docs-API%20reference-blue?style=for-the-badge&labelColor=555555&logo=dotnet" alt="Docs" height="28" /></a>
  <a href="https://scorecard.dev/viewer/?uri=github.com/sebastienrousseau/pain001-sdk-dotnet"><img src="https://img.shields.io/ossf-scorecard/github.com/sebastienrousseau/pain001-sdk-dotnet?style=for-the-badge&label=OpenSSF%20Scorecard&logo=openssf" alt="OpenSSF Scorecard" height="28" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-Apache--2.0%20OR%20MIT-blue.svg?style=for-the-badge" alt="License: Apache-2.0 OR MIT" height="28" /></a>
  <a href="https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/docs/POLICIES.md"><img src="https://img.shields.io/badge/.NET-10-93450a.svg?style=for-the-badge&logo=dotnet" alt=".NET 10 or later" height="28" /></a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/sebastienrousseau/pain001-sdk-dotnet/main/.github/demo.gif" alt="The Basic example using the .NET SDK against pain001 serve on loopback: health check, validation of the conformance kit's synthetic CSV, and generation of an XSD-valid pain.001.001.09 file" width="100%" />
</p>

---

## Contents

**Getting started**

- [Install](#install): NuGet (`Pain001.Sdk`), or a project reference from source
- [Requirements](#requirements): toolchain floor, platforms
- [Quick Start](#quick-start): generate a pain.001 file from a CSV in a few lines

**The pain001-sdk-dotnet ecosystem**

- [The pain001-sdk-dotnet ecosystem](#the-pain001-sdk-dotnet-ecosystem): the pain001 server, its five SDKs and the conformance kit

**Library reference**

- [Capabilities at a glance](#capabilities-at-a-glance): the current surface by theme
- [Ecosystem comparison](#ecosystem-comparison): short matrix; full table at [`docs/COMPARISON.md`](docs/COMPARISON.md)
- [Benchmarks](#benchmarks): headline numbers; full table at [`docs/BENCHMARKS.md`](docs/BENCHMARKS.md)
- [Features](#features): module-level capability list
- [Configuration](#configuration): core options
- [Examples](#examples): runnable example index

**Operational**

- [When not to use pain001-sdk-dotnet](#when-not-to-use-pain001-sdk-dotnet): limitations
- [Development](#development): make targets, fuzzing, CI
- [Security](#security): guarantees and compliance
- [Documentation](#documentation): all reference docs
- [Stability guarantees](#stability-guarantees): SemVer axis, output stability, minimum toolchain discipline
- [License](#license)

---

## Install

### As a .NET library

```xml
<PackageReference Include="Pain001.Sdk" Version="0.0.88" />
```

Or `dotnet add package Pain001.Sdk --version 0.0.88`. 0.0.88 is the
first release; until it is on NuGet, reference the project from a
clone:

```xml
<ProjectReference Include="path/to/pain001-sdk-dotnet/src/Pain001.Sdk/Pain001.Sdk.csproj" />
```

The SDK calls a running pain001 server, the Python package `pain001`
with its `api` extra:

```sh
pip install 'pain001[api]==0.0.87'
pain001 serve --host 127.0.0.1 --port 8000
```

---

## Requirements

- .NET 10 (`net10.0`), the supported LTS release (to November 2028).
  .NET 8 and .NET 9 reached end of support on 10 November 2026 and are
  not targeted ([policy](docs/POLICIES.md)).
- No other package: the library depends on the base class library only.
- A pain001 server reachable over HTTP. CI tests against pain001 0.0.87,
  the latest released core.
- CI builds and tests on Linux, Windows and macOS.

---

## Quick Start

```csharp
using Pain001.Sdk;

using var client = new Pain001Client(new Uri("http://127.0.0.1:8000"));

string csv = await File.ReadAllTextAsync("payments.csv");
try
{
    UiGenerateResponse result = await client.GenerateAsync(csv, "payments.csv", MessageTypes.Pain001V09);
    await File.WriteAllTextAsync("payments.xml", result.Xml);
}
catch (Pain001GenerationRefusedException refused)
{
    foreach (RowError row in refused.Response.ValidationErrors ?? [])
    {
        Console.Error.WriteLine($"{row.Field}: {row.Message}");
    }
}
```

The client posts the file's text to `POST /api/v1/ui/generate`; the
server parses it in memory, validates every row and returns the XML
inline. When the server refuses the data (a row fails validation or a
scheme rule), `GenerateAsync` raises `Pain001GenerationRefusedException`,
whose `Response` holds the findings; any other status outside 2xx raises
`Pain001ApiException` with the HTTP status and the server's `detail`.

---

## The pain001-sdk-dotnet ecosystem

The pain001 server is the one engine. Each SDK is a thin typed client
for its REST API, and the same conformance kit, vendored into every SDK,
proves each client against a running server.

| Component | Purpose | Use case |
| :--- | :--- | :--- |
| [pain001](https://github.com/sebastienrousseau/pain001) | The engine: generates and validates pain.001 and pain.008, and serves the REST API | Run `pain001 serve` for this SDK to call |
| pain001-sdk-dotnet | This repository: the .NET client | Call the API from C#, F# or any .NET language |
| [pain001-sdk-go](https://github.com/sebastienrousseau/pain001-sdk-go), [-java](https://github.com/sebastienrousseau/pain001-sdk-java), [-rust](https://github.com/sebastienrousseau/pain001-sdk-rust), [-cpp](https://github.com/sebastienrousseau/pain001-sdk-cpp) | The same client surface in Go, Java, Rust and C++ | Call the API from those languages |
| `conformance/` | The shared conformance kit, vendored from core | Prove this client against the server it describes |

---

## Capabilities at a glance

| Area | Capability | Status |
| :--- | :--- | :--- |
| Health | `HealthAsync`: server status and version | Available |
| Validation | `ValidateAsync`: row counts, row errors and scheme violations for a CSV, JSON or JSON Lines file | Available |
| Generation | `GenerateAsync`: the XML document for a payment file; refusals with their findings | Available |
| Checking | `CheckAsync`: the XSD, MDR, scheme and Schematron ladder on an existing document | Available |
| Faces | `FacesAsync`, `ProjectFaceAsync`: project a pain.001 into another format, with a loss report | Available |
| Low level | `RequestAsync`: any method and path, status and raw JSON | Available |
| Resilience | Per-request timeout, cancellation, opt-in `RetryPolicy` for `GET` calls | Available |
| Publication | NuGet package `Pain001.Sdk` | Release workflow ready; first publication with 0.0.88 |

---

## Ecosystem comparison

The suite's five SDKs share one method surface and run core's
conformance kit against a live `pain001 serve` in CI. Core's CI also
generates a Python client from the OpenAPI document, which it imports
but does not run against the kit.

| Project | Conformance kit in CI | Runtime dependencies | Call model |
| :--- | :---: | :---: | :---: |
| pain001-sdk-go | Yes | None (standard library) | Blocking, `context.Context` |
| pain001-sdk-java | Yes | Jackson databind | Blocking |
| pain001-sdk-rust | Yes | ureq, serde, serde_json, thiserror | Blocking |
| **pain001-sdk-dotnet** | **Yes** | **None** | **async/await, `CancellationToken`** |
| pain001-sdk-cpp | Yes | libcurl, nlohmann/json | Blocking |
| openapi-generator client (core CI artifact) | No, CI imports it only | Generator's | Blocking |
| Raw HTTP | No | Your HTTP client | Yours |

See [`docs/COMPARISON.md`](docs/COMPARISON.md) for the evidence and complete matrix.

---

## Benchmarks

What the SDK itself costs per call (request building and response
decoding with the kit's fixtures, no network) and one loopback round
trip to `pain001 serve` 0.0.87, measured with BenchmarkDotNet. The
client's own cost is microseconds; the round trip is the server's work.

| Scenario | Result | Environment |
| :--- | ---: | :--- |
| `GenerateAsync`, client side | 30.9 μs, 28.7 KB allocated | Apple A18 Pro, macOS 27.2, .NET 10.0.15 |
| `ProjectFaceAsync`, client side | 50.1 μs, 49.3 KB allocated | Apple A18 Pro, macOS 27.2, .NET 10.0.15 |
| `HealthAsync`, client side | 1.9 μs, 2.8 KB allocated | Apple A18 Pro, macOS 27.2, .NET 10.0.15 |
| `GenerateAsync`, loopback round trip | 55.6 ms mean, 49.7 ms median | Same machine, pain001 serve 0.0.87 on 127.0.0.1 |
| `ValidateAsync`, loopback round trip | 11.8 ms mean, 10.0 ms median | Same machine, pain001 serve 0.0.87 on 127.0.0.1 |

See [`docs/BENCHMARKS.md`](docs/BENCHMARKS.md) for methodology and full results.

---

## Features

- `Pain001Client`: one async method per endpoint, each taking a
  `CancellationToken`. Construct it with your own `HttpClient` (for
  `IHttpClientFactory`) or with a base address alone.
- Typed responses: `HealthResponse`, `ValidationResponse`,
  `UiGenerateResponse`, `CheckResponse`, `FaceInfo`, `FaceResponse` and
  `LossReport`, mapped from the server's snake_case JSON. Row errors in a
  payment file are `RowError` (`field`, `message`, `value`); the items
  of an HTTP 422 `detail` deserialize as `ValidationError`.
- `MessageTypes`: constants for the 13 message types the server lists.
  Every method takes the message type as a string, so a newer server's
  types work before this list knows them.
- Errors by cause ([ADR 0003](docs/adr/0003-error-model.md)):
  `Pain001ApiException` (a status outside 2xx, with `StatusCode` and
  `Detail`); `Pain001GenerationRefusedException`, derived from it, for a
  refused generate call, with the findings in `Response`;
  `Pain001NoDocumentException` for a 2xx generate answer without the
  XML, so a returned result always has it. Messages never carry the
  detail or a finding.
- `RetryPolicy`: opt-in retries for `GET` calls only, off by default
  ([ADR 0004](docs/adr/0004-opt-in-retries-for-get-only.md)).
- `RequestAsync`: the low-level call the conformance runner uses. It
  returns any status instead of throwing.
- No logging, and no third-party runtime dependency.

---

## Configuration

```csharp
var options = new Pain001ClientOptions
{
    BaseAddress = new Uri("https://pain001.internal.example"),
    ApiKey = Environment.GetEnvironmentVariable("PAIN001_API_KEY"),
    Timeout = TimeSpan.FromSeconds(10),
    Retry = new RetryPolicy { MaxAttempts = 3 },
};
using var client = new Pain001Client(httpClient, options);
```

| Option | Default | Meaning |
| :--- | :--- | :--- |
| `BaseAddress` | `http://127.0.0.1:8000` | The server's absolute address; a path prefix is kept |
| `ApiKey` | none | Sent as `Authorization: Bearer <key>` |
| `Timeout` | 30 seconds | Per request; exceeding it raises `TimeoutException` |
| `Retry` | none | A `RetryPolicy` for `GET` calls only (health, faces, a raw `GET`): `MaxAttempts` (3), `InitialDelay` (200 ms), `MaxDelay` (5 s); retries 429, 503 and connection errors with jittered exponential backoff, honouring `Retry-After`. A `POST` is never retried |
| `TimeProvider` | `TimeProvider.System` | The clock for retry waits and `Retry-After` dates |

The client never changes the settings of an `HttpClient` you pass in,
and does not dispose it.

---

## Examples

Each example is a console project that checks its own result. CI runs
every one against `pain001 serve` (`scripts/run-examples.sh`); run them
from the repository root, with `PAIN001_BASE_URL` pointing at a server
(default `http://127.0.0.1:8000`) and, optionally, `PAIN001_API_KEY`.

| Example | Shows | Run it |
| :--- | :--- | :--- |
| [`Basic`](examples/Basic/Program.cs) | Health, validation and generation in one go: the README demo | `dotnet run --project examples/Basic` |
| [`Health`](examples/Health/Program.cs) | `HealthAsync`: server status and version | `dotnet run --project examples/Health` |
| [`Validate`](examples/Validate/Program.cs) | `ValidateAsync`: row counts, a rejected IBAN (HTTP 400), the `uk-chaps` scheme's violations | `dotnet run --project examples/Validate` |
| [`Generate`](examples/Generate/Program.cs) | `GenerateAsync`: write the XML to a file | `dotnet run --project examples/Generate -- out.xml` |
| [`Check`](examples/Check/Program.cs) | `CheckAsync`: the validation ladder on an existing document | `dotnet run --project examples/Check` |
| [`Faces`](examples/Faces/Program.cs) | `FacesAsync`: the faces and their bindings | `dotnet run --project examples/Faces` |
| [`ProjectFace`](examples/ProjectFace/Program.cs) | `ProjectFaceAsync`: a face payload and its loss report | `dotnet run --project examples/ProjectFace -- berlin_group` |
| [`RawRequest`](examples/RawRequest/Program.cs) | `RequestAsync`: any method and path, status returned, never thrown | `dotnet run --project examples/RawRequest` |
| [`ErrorHandling`](examples/ErrorHandling/Program.cs) | `Pain001GenerationRefusedException`, `Pain001ApiException` and `Pain001NoDocumentException`, caught in the right order | `dotnet run --project examples/ErrorHandling` |
| [`Timeouts`](examples/Timeouts/Program.cs) | `Timeout`, a `CancellationToken` and the opt-in `RetryPolicy` | `dotnet run --project examples/Timeouts` |
| [`ApiKey`](examples/ApiKey/Program.cs) | `ApiKey` sent as a bearer token; a wrong key refused with HTTP 401 | `PAIN001_API_KEY=... dotnet run --project examples/ApiKey` |

---

## When not to use pain001-sdk-dotnet

- **You cannot run the pain001 server.** This SDK does not generate or
  validate anything itself; every call goes to `pain001 serve`.
- **You need a framework older than .NET 10.** The package targets
  `net10.0` only.
- **You need payment calls retried.** The opt-in `RetryPolicy` covers
  `GET` calls only; the SDK never resends a `POST` (validate, generate,
  check, a face projection). Add your own policy if you need one, knowing
  a retried generation is a second request.
- **You want an offline ISO 20022 library.** Use the server, or a
  library that builds the XML in process; this is a client.

---

## Development

```bash
make check          # format, build (analysers, complexity), 100% coverage, audit, versions, Halstead
make conformance    # the kit and typed tests; needs PAIN001_BASE_URL
make examples       # run every example against PAIN001_BASE_URL
make bench          # BenchmarkDotNet; loopback too when PAIN001_BASE_URL is set
make regression     # all of it against a server it installs and starts
make docs           # the manual, strict, with the API reference
make docs-lint      # markdownlint, codespell, REUSE
```

The build treats warnings as errors with the `latest-recommended` .NET
analysers; CA1502 and SonarAnalyzer hold every method to cyclomatic
complexity 10, cognitive complexity 15 and 60 lines, and
`scripts/halstead.py` to Halstead difficulty 30. Unit tests run under
coverlet and fail below 100 % line and branch coverage; FsCheck
properties drive every method with arbitrary server answers. There are
no fuzz targets: the client parses only JSON, through
`System.Text.Json`, and the properties cover its decoding. CI runs
format, build and tests on Linux, Windows and macOS, the
vulnerable-package audit, the conformance kit, every example and a
benchmark smoke run against a hash-pinned `pain001 serve`.
[DEVELOPMENT.md](DEVELOPMENT.md) maps every gate to its local command.

---

## Security

The SDK logs nothing and never puts a request body in an exception
message, because request bodies hold payment data: an exception's
message names the status (and, for a refusal, the count of findings),
while the server's detail stays in `Detail` and `Response`. It has no
third-party runtime dependency, sends the API key only as a bearer
token to the configured server, bounds every request with a timeout,
and never retries a `POST`. A response that is not JSON, or not valid
UTF-8, becomes a string, never an exception from inside the parser.

Report vulnerabilities according to [`SECURITY.md`](SECURITY.md).

---

## Documentation

- [User manual](https://sebastienrousseau.github.io/pain001-sdk-dotnet/),
  built from [`docs/`](docs/index.md).
- [API reference](https://sebastienrousseau.github.io/pain001-sdk-dotnet/api/),
  generated from the XML documentation comments, which also ship in the
  package for IDE tooltips.
- [Developer docs](DEVELOPMENT.md): toolchain, every CI gate, test
  layout, release model.
- [Ecosystem map](docs/COMPARISON.md): the suite's SDKs side by side.
- [`conformance/README.md`](conformance/README.md): the conformance kit
  and its case format.

---

## Stability guarantees

The version is `0.0.x` and moves by `0.0.1` in lockstep with the pain001
suite; before `1.0` any release may change the API, and the
[CHANGELOG](CHANGELOG.md) says how. Each release runs package validation
(ApiCompat) against the previous published package, so no break is
accidental. Output stability: the same server answer decodes to the same
records and raises the same exception types; a change to that mapping
is breaking even when no signature moves. SDK version 0.0.N vendors the
conformance kit from core 0.0.N, and CI fails if any case fails against
the server. The `net10.0` floor moves only with a release that says so
([policy](docs/POLICIES.md)).

---

## License

Dual-licensed under [Apache-2.0](LICENSE-APACHE) or [MIT](LICENSE-MIT),
at your option. See [LICENSE](LICENSE). Every file carries its licence
in machine-readable form ([REUSE](https://reuse.software/), checked in
CI).
