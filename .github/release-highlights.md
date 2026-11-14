<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->
<!--
The hand-written Highlights of the next GitHub release: 2 to 4 bullets,
each "* **Headline**: one or two plain sentences.", no em dashes.
scripts/release-notes.sh puts them under "## Highlights ⭐️" and composes
the rest. Rewrite it in the release preparation of every version.
-->
* **First release on NuGet**: `Pain001.Sdk` 0.0.88, a typed async client for `pain001 serve` with no runtime dependency, in lockstep with the pain001 suite.
* **Refusals with their findings**: a generate call the server refuses raises `Pain001GenerationRefusedException`, whose `Response` lists the failing rows and scheme rules; a 2xx answer without XML raises `Pain001NoDocumentException`.
* **Opt-in retries for GET calls**: `RetryPolicy` retries health, faces and raw `GET` requests on 429, 503 and connection errors, honouring `Retry-After`. A `POST` is never retried.
* **Proven against the server**: conformance kit 2, an example per method and BenchmarkDotNet runs, all against `pain001 serve` in CI, with 100 % line and branch coverage.
