<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# ADR 0002: No third-party runtime dependency

- Status: Accepted
- Date: 2026-11-10 (recorded 2026-11-14)

## Context

The package runs inside payment systems, where every dependency is a
review, a licence check and a supply-chain risk. .NET's base class
library has an HTTP client (`HttpClient`), JSON (`System.Text.Json`) and
the time abstraction (`TimeProvider`) the client needs.

## Decision

`Pain001.Sdk` depends on the base class library only. Packages used by
tests, coverage and benchmarks (`xunit`, `FsCheck`, `coverlet.msbuild`,
`BenchmarkDotNet`) are pinned exactly in `Directory.Packages.props`,
locked with content hashes in `packages.lock.json`, and never ship in
the package.

## Consequences

- `dotnet list package --include-transitive` on the library lists
  nothing; the vulnerable-package audit covers the test graph.
- Features a library would give for free (Polly-style resilience,
  logging) are small and written here, as `RetryPolicy` was
  ([ADR 0004](0004-opt-in-retries-for-get-only.md)), or left to the
  caller's own `HttpClient` pipeline (`IHttpClientFactory`).
