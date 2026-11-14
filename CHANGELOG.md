<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
This package's version follows the [`pain001`](https://github.com/sebastienrousseau/pain001)
suite; the first release will be `0.0.86`, in lockstep with it.

## [Unreleased]

### Changed

- The README shows a demo, `.github/demo.gif`, rendered by `make demo`
  from `.github/demo.tape`: the Basic example against a real
  `pain001 serve` on loopback. `examples/Basic` now also prints the
  server's health and the validation row counts before it generates.

- Tests run on xunit.runner.visualstudio 4.0.1 (was 3.1.5) and
  Microsoft.NET.Test.Sdk 18.12.0 (was 18.11.0); lock file updated.
- The CI vulnerable-package job restores with `--locked-mode`, as
  every other restore in CI already does through `RestoreLockedMode`.

### Added

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
- The conformance kit, vendored from core 0.0.86 and pinned by
  `conformance/SHA256SUMS`, with a runner that ports core's reference
  runner, typed tests for every method, and unit tests against an
  in-memory handler.
- CI: format, build and unit tests on Linux, Windows and macOS, a
  vulnerable-package audit, and a conformance job against a
  hash-pinned `pain001 serve` 0.0.86.
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

- A response body that is not valid UTF-8 is kept as text, each invalid
  byte replaced by U+FFFD, as the documentation already promised for a
  body that is not JSON. Before, `Pain001ApiException.Detail` and
  `Pain001RawResponse.Body` held an element that threw
  `InvalidOperationException` when read, so an error handler calling
  `Detail.GetString()` on a Latin-1 proxy page threw again.
