<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->
<!--
The hand-written Highlights of the next GitHub release: 2 to 4 bullets,
each "* **Headline**: one or two plain sentences.", no em dashes.
scripts/release-notes.sh puts them under "## Highlights ⭐️" and composes
the rest. Rewrite it in the release preparation of every version.
-->
* **In lockstep with pain001 0.0.91**: `Pain001.Sdk` 0.0.91 passes the conformance kit against `pain001 serve` 0.0.91 in CI. The kit is unchanged since 0.0.90, so no client code had to move.
* **Manual built with mkdocs-material 9.8.2**: it fixes GHSA-wxw8-4jgj-q85j, a path traversal in the theme's privacy plugin, and Dependabot now watches the manual's inputs.
* **API checked against 0.0.90**: package validation compares the public API with the published 0.0.90 package, and the public API is unchanged.
