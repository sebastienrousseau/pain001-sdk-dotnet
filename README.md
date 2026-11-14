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
  <a href="#install"><img src="https://img.shields.io/badge/NuGet-not%20yet%20published-lightgrey?style=for-the-badge&color=fc8d62&logo=nuget" alt="Registry" height="28" /></a>
  <a href="#documentation"><img src="https://img.shields.io/badge/docs-XML%20doc%20comments-blue?style=for-the-badge&labelColor=555555&logo=dotnet" alt="Docs" height="28" /></a>
  <a href="https://scorecard.dev/viewer/?uri=github.com/sebastienrousseau/pain001-sdk-dotnet"><img src="https://img.shields.io/ossf-scorecard/github.com/sebastienrousseau/pain001-sdk-dotnet?style=for-the-badge&label=OpenSSF%20Scorecard&logo=openssf" alt="OpenSSF Scorecard" height="28" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-Apache--2.0%20OR%20MIT-blue.svg?style=for-the-badge" alt="License: Apache-2.0 OR MIT" height="28" /></a>
  <a href="#requirements"><img src="https://img.shields.io/badge/.NET-10-93450a.svg?style=for-the-badge&logo=dotnet" alt=".NET 10" height="28" /></a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/sebastienrousseau/pain001-sdk-dotnet/main/.github/demo.gif" alt="The Basic example using the .NET SDK against pain001 serve on loopback: health check, validation of the conformance kit's synthetic CSV, and generation of an XSD-valid pain.001.001.09 file" width="100%" />
</p>

---

## Contents

**Getting started**

- [Install](#install): from source today; NuGet once released
- [Requirements](#requirements): toolchain floor, platforms
- [Quick Start](#quick-start): generate a pain.001 file from a CSV in a few lines

**The pain001-sdk-dotnet ecosystem**

- [The pain001-sdk-dotnet ecosystem](#the-pain001-sdk-dotnet-ecosystem): the pain001 server, its SDKs and the conformance kit

**Library reference**

- [Capabilities at a glance](#capabilities-at-a-glance): the current surface by theme
- [Ecosystem comparison](#ecosystem-comparison): not published yet, and why
- [Benchmarks](#benchmarks): none, and why
- [Features](#features): the client's methods and types
- [Configuration](#configuration): base address, API key, timeout
- [Examples](#examples): runnable example index

**Operational**

- [When not to use pain001-sdk-dotnet](#when-not-to-use-pain001-sdk-dotnet): limitations
- [Development](#development): make targets, CI
- [Security](#security): guarantees and compliance
- [Documentation](#documentation): all reference docs
- [Stability guarantees](#stability-guarantees): SemVer axis, output stability, minimum toolchain discipline
- [License](#license)

---

## Install

### As a .NET library

The package is **not yet on NuGet**. The first release, `0.0.88`, will
ship in lockstep with the pain001 suite. Until then, reference the
project from source:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/pain001-sdk-dotnet/src/Pain001.Sdk/Pain001.Sdk.csproj" />
</ItemGroup>
```

Once it is published, the reference will be:

```xml
<PackageReference Include="Pain001.Sdk" Version="0.0.88" />
```

You also need a running pain001 server, which is the Python package
`pain001` with its `api` extra:

```sh
pip install 'pain001[api]'
pain001 serve --host 127.0.0.1 --port 8000
```

---

## Requirements

- The library targets `net10.0`, the current LTS release (supported to
  November 2028). .NET 8 and .NET 9 reached end of support on
  10 November 2026, so they are not targeted.
- Building the repository needs the .NET 10 SDK.
- A pain001 server (`pain001 serve`) reachable over HTTP. CI tests
  against pain001 0.0.87, the latest released core.
- CI builds and tests on Linux, Windows and macOS.

---

## Quick Start

```csharp
using Pain001.Sdk;

using var client = new Pain001Client(new Uri("http://127.0.0.1:8000"));

string csv = await File.ReadAllTextAsync("payments.csv");
UiGenerateResponse result = await client.GenerateAsync(
    csv, "payments.csv", MessageTypes.Pain001V09);

if (result.Success)
{
    await File.WriteAllTextAsync("payments.xml", result.Xml);
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
| `conformance/` | The shared conformance kit, vendored from core | Prove this client against the server it describes |

---

## Capabilities at a glance

| Area | Capability | Status |
| :--- | :--- | :--- |
| Health | `HealthAsync`: server status and version | Available |
| Validation | `ValidateAsync`: row counts and errors for a CSV, JSON or JSON Lines file | Available |
| Generation | `GenerateAsync`: the XML document for a payment file | Available |
| Checking | `CheckAsync`: the XSD, MDR, scheme and Schematron ladder on an existing document | Available |
| Faces | `FacesAsync`, `ProjectFaceAsync`: project a pain.001 into another format, with a loss report | Available |
| Low level | `RequestAsync`: any method and path, status and raw JSON | Available |
| Publication | NuGet package | Not yet published |

---

## Ecosystem comparison

Not published yet. A comparison with other .NET ISO 20022 libraries
needs evidence this repository does not have; it will be added with
that evidence rather than guessed.

| Project | Typed REST client | Conformance kit in CI | Third-party runtime dependencies |
| :--- | :---: | :---: | :---: |
| **pain001-sdk-dotnet** | Yes | Yes | None |

---

## Benchmarks

None. Every call is one HTTP request to the server, so the time is the
server's and the network's; a client-side benchmark would measure
neither.

| Scenario | Result | Environment |
| :--- | ---: | :--- |
| Not measured | n/a | n/a |

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
- `Pain001ApiException`: the HTTP status and the server's `detail` (a
  string, or an array of validation errors for HTTP 422).
  `Pain001GenerationRefusedException` derives from it and carries a
  refused generate call's findings in `Response`.
- `Pain001NoDocumentException`: a 2xx generate answer without the XML
  (an older or broken server), so a returned result always has it.
- `RequestAsync`: the low-level call the conformance runner uses. It
  returns any status instead of throwing.
- No retries, no logging, and no third-party runtime dependency.

---

## Configuration

```csharp
var options = new Pain001ClientOptions
{
    BaseAddress = new Uri("https://pain001.internal.example"),
    ApiKey = Environment.GetEnvironmentVariable("PAIN001_API_KEY"),
    Timeout = TimeSpan.FromSeconds(10),
};
using var client = new Pain001Client(httpClient, options);
```

| Option | Default | Meaning |
| :--- | :--- | :--- |
| `BaseAddress` | `http://127.0.0.1:8000` | The server's absolute address; a path prefix is kept |
| `ApiKey` | none | Sent as `Authorization: Bearer <key>` |
| `Timeout` | 30 seconds | Per request; exceeding it raises `TimeoutException` |

The client never changes the settings of an `HttpClient` you pass in,
and does not dispose it.

---

## Examples

| Example | What it does | Run it |
| :--- | :--- | :--- |
| [`examples/Basic`](examples/Basic/Program.cs) | Checks the server's health, validates the kit's synthetic credit-transfer CSV and generates a pain.001.001.09 file from it | `PAIN001_BASE_URL=http://127.0.0.1:8000 dotnet run --project examples/Basic` |

---

## When not to use pain001-sdk-dotnet

- **You cannot run the pain001 server.** This SDK does not generate or
  validate anything itself; every call goes to `pain001 serve`.
- **You need it from NuGet today.** It is not published yet.
- **You need retries.** The SDK never resends a payment request; add
  your own policy (for example with `IHttpClientFactory`) if you want
  one, knowing a retried generation is a second request.

---

## Development

```bash
make check          # format, build, unit tests, vulnerable-package audit
make conformance    # the kit and typed tests; needs PAIN001_BASE_URL
make example        # run examples/Basic against PAIN001_BASE_URL
make docs-lint      # markdownlint, as the docs-lint workflow runs it
```

The build treats warnings as errors with the `latest-recommended` .NET
analysers, and CA1502 (configured in `CodeMetricsConfig.txt`) holds
every method to a cyclomatic complexity of 10. CI runs format, build and
unit tests on Linux, Windows and macOS, the vulnerable-package audit,
and the conformance kit against a hash-pinned `pain001 serve`.

---

## Security

The SDK logs nothing and never puts a request body in an exception
message, because request bodies hold payment data. It has no
third-party runtime dependency, sends the API key only as a bearer
token to the configured server, and never retries.

Report vulnerabilities according to [`SECURITY.md`](SECURITY.md).

---

## Documentation

- Every public type and member carries XML documentation, shipped in the
  package for IDE tooltips.
- [`conformance/README.md`](conformance/README.md): the conformance kit
  and its case format.
- [`conformance/openapi.json`](conformance/openapi.json): the server's
  OpenAPI document at this kit version.
- [`CONTRIBUTING.md`](CONTRIBUTING.md), [`AGENTS.md`](AGENTS.md),
  [`SECURITY.md`](SECURITY.md), [`CHANGELOG.md`](CHANGELOG.md).

---

## Stability guarantees

The version is `0.0.x` and moves by `0.0.1` in lockstep with the pain001
suite; before `1.0` any release may change the API, and the
[CHANGELOG](CHANGELOG.md) says how. SDK version 0.0.N vendors the
conformance kit from core 0.0.N, and CI fails if any case fails against
the server. The `net10.0` floor moves only with a release that says so.

---

## License

Dual-licensed under [Apache-2.0](LICENSE-APACHE) or [MIT](LICENSE-MIT),
at your option. See [LICENSE](LICENSE).
