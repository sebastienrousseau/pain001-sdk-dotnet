<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Ecosystem comparison

The suite's five SDKs share one method surface and run core's
conformance kit against a live `pain001 serve` in CI. Core's CI also
generates a Python client from the OpenAPI document, which it imports
but does not run against the kit.

| Project | Language (minimum) | Conformance kit in CI | Types | Runtime dependencies | Call model |
| :--- | :---: | :---: | :---: | :--- | :--- |
| pain001-sdk-go | Go 1.26 | Yes | Hand-written | None (standard library) | Blocking, `context.Context` |
| pain001-sdk-java | Java 17 | Yes | Hand-written | Jackson databind | Blocking |
| pain001-sdk-rust | Rust 1.85 | Yes | Hand-written (serde) | ureq, serde, serde_json, thiserror | Blocking |
| **pain001-sdk-dotnet** | **.NET 10** | **Yes** | **Hand-written** | **None** | **async/await, `CancellationToken`** |
| pain001-sdk-cpp | C++17 | Yes | Hand-written | libcurl, nlohmann/json | Blocking |
| openapi-generator client (core CI artifact) | Python | No, CI imports it only | Generated | Generator's | Blocking |
| Raw HTTP | Any | No | None | Your HTTP client | Yours |

## Evidence

| Project | Conformance job | Manifest |
| :--- | :--- | :--- |
| pain001-sdk-go | [`ci.yml`, job `conformance`](https://github.com/sebastienrousseau/pain001-sdk-go/blob/main/.github/workflows/ci.yml) | [`go.mod`](https://github.com/sebastienrousseau/pain001-sdk-go/blob/main/go.mod) |
| pain001-sdk-java | [`ci.yml`, job `conformance`](https://github.com/sebastienrousseau/pain001-sdk-java/blob/main/.github/workflows/ci.yml) | [`pom.xml`](https://github.com/sebastienrousseau/pain001-sdk-java/blob/main/pom.xml) |
| pain001-sdk-rust | [`ci.yml`, job `conformance`](https://github.com/sebastienrousseau/pain001-sdk-rust/blob/main/.github/workflows/ci.yml) | [`Cargo.toml`](https://github.com/sebastienrousseau/pain001-sdk-rust/blob/main/Cargo.toml) |
| pain001-sdk-dotnet | [`ci.yml`, job `conformance`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/.github/workflows/ci.yml) | [`Pain001.Sdk.csproj`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/src/Pain001.Sdk/Pain001.Sdk.csproj), [`Directory.Packages.props`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/Directory.Packages.props) |
| pain001-sdk-cpp | [`ci.yml`, job `conformance`](https://github.com/sebastienrousseau/pain001-sdk-cpp/blob/main/.github/workflows/ci.yml) | [`CMakeLists.txt`](https://github.com/sebastienrousseau/pain001-sdk-cpp/blob/main/CMakeLists.txt) |
| openapi-generator client | [core `sdk.yml`](https://github.com/sebastienrousseau/pain001/blob/main/.github/workflows/sdk.yml): generated, built and imported; no kit run | n/a |

## This SDK's row

- **.NET 10**: `<TargetFramework>net10.0</TargetFramework>` in the
  csproj; [ADR 0005](adr/0005-target-net10-only.md).
- **Conformance kit in CI**: the `conformance` job runs every case in
  `conformance/cases.json` through `RequestAsync`, plus typed tests and
  every example, against the pinned `pain001 serve`.
- **Hand-written types**: the records in `src/Pain001.Sdk/Models/`.
- **No runtime dependency**: the csproj has no `PackageReference`;
  [ADR 0002](adr/0002-no-third-party-runtime-dependencies.md).
- **async/await, `CancellationToken`**: every method returns a `Task`
  and takes a token.
