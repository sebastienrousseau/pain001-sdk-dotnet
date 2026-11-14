<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# ADR 0005: Target net10.0 only

- Status: Accepted
- Date: 2026-11-10 (recorded 2026-11-14)

## Context

.NET 8 and .NET 9 reached end of support on 10 November 2026. .NET 10
is the supported LTS release, to November 2028. Multi-targeting an
unsupported framework means testing on a runtime that no longer gets
security fixes.

## Decision

The library targets `net10.0` only. CI builds and tests it on Linux,
Windows and macOS with the .NET 10 SDK.

## Consequences

- The floor moves with .NET's LTS cycle, in a release whose CHANGELOG
  says so, never as a side effect.
- `TimeProvider`, `System.Text.Unicode.Utf8` and the collection
  expressions the code uses need no polyfills.
