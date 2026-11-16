<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->
<!--
The hand-written Highlights of the next GitHub release: 2 to 4 bullets,
each "* **Headline**: one or two plain sentences.", no em dashes.
scripts/release-notes.sh puts them under "## Highlights ⭐️" and composes
the rest. Rewrite it in the release preparation of every version.
-->
* **In lockstep with pain001 0.0.90**: `Pain001.Sdk` 0.0.90 passes the conformance kit against `pain001 serve` 0.0.90 in CI. The kit is unchanged from 0.0.88, so no client code had to move.
* **No 0.0.89 for .NET**: core 0.0.89's server returned HTTP 500 to parallel conformance requests, a race that core 0.0.90 fixes, so the SDK goes from 0.0.88 straight to 0.0.90.
* **API breakage checked against the last release**: package validation now compares the public API with the published 0.0.88 package, so a breaking change fails the build instead of shipping.
* **Every release asset in the checksums**: the release notes list `SHA256SUMS` itself alongside the packages and SBOM.
