<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
This package's version follows the [`pain001`](https://github.com/sebastienrousseau/pain001)
suite; the first release will be `0.0.88`, in lockstep with it.

## [Unreleased]

### Changed

- The CI and CodeQL workflows run in concurrency groups: a
  new push to a pull request cancels the superseded run.

- `dotnet pack` also writes a symbols package (`.snupkg`) and runs .NET
  package validation, which uses ApiCompat. `scripts/api-compat.sh`
  (the CI Linux build job) adds the last published `Pain001.Sdk` as the
  baseline, so a breaking API change fails; until the first release is
  published it says there is no baseline and checks the package alone.

- The package version is 0.0.88, the suite version this first release
  ships with (`Pain001.Sdk.csproj`, `CITATION.cff`, the `SECURITY.md`
  table, the README install snippet and the bug report form).

- The vendored conformance kit is kit 2, byte-identical to core's
  (pinned by `conformance/SHA256SUMS`): new cases
  `generate-refused-rows`, `generate-refused-scheme`,
  `validate-bad-lei` and `validate-chaps-purpose-required`, and their
  fixtures. The CI conformance job runs it against the latest released
  core, `pain001 serve` 0.0.87, re-locked with hashes in
  `ci/server-requirements.txt`.

- The README shows a demo, `.github/demo.gif`, rendered by `make demo`
  from `.github/demo.tape`: the Basic example against a real
  `pain001 serve` on loopback. `examples/Basic` now also prints the
  server's health and the validation row counts before it generates.

- Tests run on xunit.runner.visualstudio 4.0.1 (was 3.1.5) and
  Microsoft.NET.Test.Sdk 18.12.0 (was 18.11.0); lock file updated.
- The CI vulnerable-package job restores with `--locked-mode`, as
  every other restore in CI already does through `RestoreLockedMode`.

### Added

- REUSE compliance: `LICENSES/` holds the Apache-2.0 and MIT texts (as
  in `LICENSE-APACHE` and `LICENSE-MIT`), and `REUSE.toml` covers the
  files that cannot carry a header. The docs-lint workflow (and `make
  docs-lint`) runs `reuse lint` and codespell beside markdownlint, from
  hash-pinned `ci/lint-requirements.txt`.

- BenchmarkDotNet benchmarks (`benchmarks/Pain001.Sdk.Benchmarks`,
  BenchmarkDotNet 0.15.8): the client-side cost of every typed method and
  `RequestAsync` with the kit's fixtures and recorded server answers, no
  network; and one loopback round trip per method to `pain001 serve`
  when `PAIN001_BASE_URL` is set. `make bench` runs them; `make
  bench-smoke` (one iteration each) runs in the CI conformance job.
  Lock file added.

- The complexity ceilings of the portfolio standard, enforced on every
  method by the build and CI: cyclomatic complexity 10 (CA1502, as
  before), cognitive complexity 15, 60 lines per method and 500 per file
  (SonarAnalyzer.CSharp 10.35.0.4138, a build-time analyser with only
  S3776, S138 and S104 switched on; thresholds in `SonarLint.xml`), and
  Halstead difficulty 30 (`scripts/halstead.py`, `make halstead`, the CI
  Complexity job). No method is over a ceiling. Lock files updated.

- One runnable example per public method in `examples/`: `Health`,
  `Validate`, `Generate`, `Check`, `Faces`, `ProjectFace`, `RawRequest`,
  `ErrorHandling` (refusal, API error and no-document, in catch order),
  `Timeouts` (timeout, cancellation and `RetryPolicy`) and `ApiKey`,
  beside `Basic`. Each checks its own result. `scripts/run-examples.sh`
  (`make examples`) runs them all; the CI conformance job runs it
  against `pain001 serve`, plus `ApiKey` against a second server
  started with `PAIN001_API_KEY`.

- A coverage gate: `make coverage` (part of `make check`) and the CI
  unit-test job run the tests under coverlet (`coverlet.msbuild`
  10.1.0, test-only) and fail below 100 % line and branch coverage of
  the library. Lock file updated.

- `RetryPolicy`, an opt-in retry policy set on
  `Pain001ClientOptions.Retry` (default none, so behaviour is
  unchanged). It resends `GET` calls only (`HealthAsync`, `FacesAsync`,
  `RequestAsync` with `GET`) on HTTP 429 or 503 and on connection
  errors, up to `MaxAttempts` (3), with exponential backoff and jitter
  from `InitialDelay` (200 ms) capped at `MaxDelay` (5 s). A
  `Retry-After` header replaces the backoff, and one asking for more
  than `MaxDelay` ends the retries. A `POST` is never retried.
  `Pain001ClientOptions.TimeProvider` sets the clock it waits on.

- `Pain001GenerationRefusedException`, a `Pain001ApiException`
  subclass that `GenerateAsync` raises when the server refuses the data
  (HTTP 422 with `success: false`, pain001 0.0.87 and later). Its
  `Response` holds the decoded body, with the blocking
  `ValidationErrors` and `SchemeViolations`; its message gives the
  status and the count of each, never the findings. A 422 for a
  malformed request stays a plain `Pain001ApiException`. This matches
  the Go SDK's `RefusalError` and the Java SDK's
  `GenerationRefusedException`. `Pain001ApiException` is no longer
  sealed, so a `catch (Pain001ApiException)` still catches a refusal.

- Target `net10.0` only. .NET 8 and .NET 9 reached end of support on
  10 November 2026; .NET 10 is the supported LTS release (to November
  2028).
- `Pain001Client`, a typed async client for the pain001 REST API:
  `HealthAsync`, `ValidateAsync`, `GenerateAsync`, `CheckAsync`,
  `FacesAsync`, `ProjectFaceAsync` and the low-level `RequestAsync`,
  with `CancellationToken` on every call, an optional bearer API key and
  a per-request timeout. No third-party runtime dependency.
- `Pain001ApiException`, carrying the HTTP status and the server's
  `detail`; its message never includes the detail, which can echo
  payment data.
- Response records with snake_case JSON mapping (row errors as
  pain001's `RowError` `{field, message, value}`; HTTP 422 items as
  FastAPI's `ValidationError`), and `MessageTypes`
  constants for the 13 message types the server lists.
- The conformance kit, vendored from core and pinned by
  `conformance/SHA256SUMS`, with a runner that ports core's reference
  runner, typed tests for every method, and unit tests against an
  in-memory handler.
- CI: format, build and unit tests on Linux, Windows and macOS, a
  vulnerable-package audit, and a conformance job against a
  hash-pinned `pain001 serve`.
- An example console project, `examples/Basic`, that generates a
  pain.001.001.09 file from the kit's credit-transfer fixture.
- Community baseline: README, SECURITY, CONTRIBUTING, CODE_OF_CONDUCT,
  AGENTS, CITATION, DCO, issue forms, a pull request template, and DCO
  and markdownlint workflows.
- FsCheck property tests over what the server sends back
  (`ResponsePropertyTests`): any status, bytes and media type through
  every typed method and `RequestAsync`, error details that must stay
  out of the exception message, and well-formed answers that must
  decode to what was sent. 500 cases per property, in the existing unit
  test job. Test-only packages FsCheck and FsCheck.Xunit 3.6.0; lock
  file updated.

### Fixed

- `GenerateAsync` no longer returns a result without a document. A 2xx
  answer whose `success` is not true, or whose `xml` is missing or
  empty, now raises `Pain001NoDocumentException` (with the status and
  the decoded `Response`), as Go returns `ErrNoDocument`, C++ throws
  `NoDocument` and Rust returns `Error::Decode`. A returned
  `UiGenerateResponse` therefore always carries the XML.

- A response body that is not valid UTF-8 is kept as text, each invalid
  byte replaced by U+FFFD, as the documentation already promised for a
  body that is not JSON. Before, `Pain001ApiException.Detail` and
  `Pain001RawResponse.Body` held an element that threw
  `InvalidOperationException` when read, so an error handler calling
  `Detail.GetString()` on a Latin-1 proxy page threw again.
