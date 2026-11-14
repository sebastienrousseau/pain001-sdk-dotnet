<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# ADR 0001: A thin client over the server

- Status: Accepted
- Date: 2026-11-10 (recorded 2026-11-14)

## Context

pain001 generates and validates ISO 20022 pain.001 and pain.008 files.
The rules (XSD, Message Definition Reports, scheme rulebooks, identifier
checks) live in the Python engine, and change with each release. A .NET
user needs them without running Python in process.

A port of the engine to .NET would be a second implementation of every
rule, which would drift from the first; a generated OpenAPI client would
type the wire but prove nothing about it.

## Decision

The SDK is a thin, hand-written client for the REST API that
`pain001 serve` exposes. It builds requests, sends them with
`HttpClient` and decodes the answers into records. It validates nothing
itself.

Core's conformance kit (`conformance/`), vendored byte for byte and
pinned by `conformance/SHA256SUMS`, is the contract: CI runs every case
through `RequestAsync` against a live server at the pinned version, and
typed tests exercise every method against the same server.

## Consequences

- One engine: a rule fixed in pain001 is fixed for every SDK, with no
  release of this package.
- The user runs a server (`pain001 serve`, or a container) and the
  network is part of every call; [the benchmarks](../BENCHMARKS.md)
  show the client's own share is small.
- The kit decides when the client is wrong. A change to the wire is a
  coordinated suite release, never a local divergence.
