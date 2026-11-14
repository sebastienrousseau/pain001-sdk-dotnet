<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Roadmap

The SDK follows the pain001 suite: what the server learns, the SDK
carries in the same release. This page lists what is planned for the
client itself. It is a plan, not a promise; the
[CHANGELOG](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/CHANGELOG.md)
records what shipped.

## 0.0.88 (this release)

- First NuGet release of `Pain001.Sdk`, by trusted publishing, once the
  `Pain001.` package prefix reservation is granted.
- Conformance kit 2: generate refusals as
  `Pain001GenerationRefusedException`, answers without XML as
  `Pain001NoDocumentException`.
- Opt-in `RetryPolicy` for `GET` calls.
- The user manual, examples for every method, benchmarks, 100 % line
  and branch coverage, and the release workflow.

## Next

- Run the API compatibility check (ApiCompat package validation)
  against the published 0.0.88 package from the next release on.
- Typed methods for endpoints the server adds, in the release that adds
  them; until then `RequestAsync` reaches any endpoint.
- Keep the kit current: each suite release vendors core's kit and runs
  it against that release's server.

## Not planned

- Generating, validating or parsing XML in .NET: the server does it
  ([ADR 0001](adr/0001-thin-client-over-the-server.md)).
- Retrying `POST` calls ([ADR 0004](adr/0004-opt-in-retries-for-get-only.md)).
- Targeting frameworks out of Microsoft support
  ([ADR 0005](adr/0005-target-net10-only.md)).
