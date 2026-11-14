<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Development

The single entry point for working on `pain001-sdk-dotnet`: the
toolchain, every CI gate reproduced locally, the test layout and the
release model. [CONTRIBUTING.md](CONTRIBUTING.md) covers how to send a
change; [AGENTS.md](AGENTS.md) lists the rules that must not drift.

## Toolchain

| Tool | Version | Used for |
| :--- | :--- | :--- |
| .NET SDK | 10.0 (`net10.0`) | Build, tests, coverage, examples, benchmarks, pack |
| Python | 3.12 | `pain001 serve`, the manual (MkDocs), codespell, REUSE, the Halstead check |
| Node.js | 22 | markdownlint |
| uv | any recent | Re-locking `ci/*.txt` and `docs/requirements.txt` |
| vhs | any recent | Rendering `.github/demo.gif` (`make demo`) |

`.devcontainer/` has all of them: open the repository in a dev
container and `make check` works once `postCreateCommand` has built
the solution. Every NuGet package is pinned exactly in
`Directory.Packages.props` and locked with content hashes in each
project's `packages.lock.json`; CI restores in locked mode. After a
package change, regenerate the lock files with
`dotnet restore --force-evaluate` (never edit them by hand) and say so
in the commit body.

## A server

The live tests, the examples, the loopback benchmarks and
`make regression` need `pain001 serve`. Install the CI pin, hash-checked:

```sh
uv venv /tmp/pain001-srv --python 3.12
VIRTUAL_ENV=/tmp/pain001-srv uv pip install --require-hashes --no-deps -r ci/server-requirements.txt
/tmp/pain001-srv/bin/pain001 serve --host 127.0.0.1 --port 8000 &
export PAIN001_BASE_URL=http://127.0.0.1:8000
```

`ci/server-requirements.in` names the latest released core; re-lock
`ci/server-requirements.txt` with the command in its header when it
moves.

## Every CI gate, locally

| Gate | Local command | CI |
| :--- | :--- | :--- |
| Format | `make format` (`dotnet format --verify-no-changes`) | `ci.yml` Format |
| Build: warnings as errors, `latest-recommended` analysers, complexity | `make build` | `ci.yml` Build and unit tests (Linux, Windows, macOS) |
| Unit tests with 100 % line and branch coverage | `make coverage` | `ci.yml` Build and unit tests |
| Vulnerable packages | `make audit` | `ci.yml` Vulnerable packages |
| Version references agree | `make verify-versions` | `ci.yml` Release versions agree |
| API compatibility (ApiCompat) | `scripts/api-compat.sh` | `ci.yml` Build and unit tests (Linux) |
| Halstead difficulty ≤ 30 | `make halstead` | `ci.yml` Complexity |
| Conformance kit and typed tests against a server | `make conformance` | `ci.yml` Conformance |
| Every example against a server | `make examples` | `ci.yml` Conformance |
| Benchmark smoke run | `make bench-smoke` | `ci.yml` Conformance |
| The manual, strict, with the API reference | `make docs` | `docs.yml` |
| markdownlint, codespell, REUSE | `make docs-lint` | `docs-lint.yml` |
| Signed-off-by on every commit | `git log --format=%B` | `dco.yml` |
| CodeQL, OpenSSF Scorecard | n/a (GitHub only) | `codeql.yml`, `scorecard.yml` |

`make check` runs format, build, coverage, the audit, the version check
and the Halstead check: everything that needs no server.
`make regression` runs all of it against a server it starts itself.

### Complexity ceilings

The build enforces the ceilings of the portfolio standard on every
method, in the library, tests, examples and benchmarks:

| Metric | Ceiling | Enforced by |
| :--- | :--- | :--- |
| Cyclomatic complexity | 10 | CA1502 (`CodeMetricsConfig.txt`) |
| Cognitive complexity | 15 | SonarAnalyzer S3776 (`SonarLint.xml`) |
| Lines per method | 60 | SonarAnalyzer S138 |
| Lines per file | 500 | SonarAnalyzer S104 |
| Halstead difficulty | 30 | `scripts/halstead.py` |

Only those SonarAnalyzer rules are on (`.editorconfig`); the others stay
off so the analyser adds no unrelated warnings. No method is over a
ceiling, so there is no baseline.

### Coverage

`make coverage` runs the unit tests under coverlet and fails below
100 % line or branch coverage of `Pain001.Sdk`. The report is
`artifacts/coverage/coverage.cobertura.xml`. An exclusion is an inline
`[ExcludeFromCodeCoverage(Justification = "...")]` listed here, never a
filter; there are none.

## Test layout

| Path | What it covers | Needs a server |
| :--- | :--- | :--- |
| `tests/Pain001.Sdk.Tests/ClientTests.cs` | Every method against an in-memory handler: paths, headers, bodies, errors, refusals, no-document, timeouts | No |
| `RetryTests.cs` | `RetryPolicy`, with a clock that never sleeps (`InstantTimeProvider`) | No |
| `ResponsePropertyTests.cs` | FsCheck properties over any status, body and media type | No |
| `RunnerTests.cs`, `KitTests.cs` | The conformance runner, and the kit against `SHA256SUMS` | No |
| `ConformanceTests.cs` | Every case in `conformance/cases.json` | Yes |
| `TypedServerTests.cs` | One test per typed method | Yes |

Tests that need a server skip unless `PAIN001_BASE_URL` is set.

## Examples and benchmarks

`examples/` has one project per method; `scripts/run-examples.sh`
(`make examples`) runs them all and fails on the first wrong result.
`benchmarks/Pain001.Sdk.Benchmarks` is BenchmarkDotNet: `make bench`
runs the client-side benchmarks, and the loopback ones when
`PAIN001_BASE_URL` is set. [docs/BENCHMARKS.md](docs/BENCHMARKS.md)
records the method and the numbers.

## Regression run

```sh
make regression
```

`scripts/regression.sh` installs the pinned server into a temporary
virtual environment (unless `PAIN001_SERVER` names a `pain001`
executable), starts it and a second one with an API key on free
loopback ports, runs `make check`, the conformance kit, every example
and the benchmark smoke run, and stops both servers on exit, failure
included. It is safe to run again.

## The manual

`make docs` builds the MkDocs Material manual from `docs/` in strict
mode, after `scripts/api-docs.sh` renders the API reference from the
library's XML documentation comments into `docs/api/` (generated, not
committed). Root Markdown files (`DEVELOPMENT.md`, `SECURITY.md`, ...)
are included into the manual, not copied. `docs.yml` deploys it to
GitHub Pages from `main`.

## Release model

The version is `0.0.x`, moves by `0.0.1`, and ships in lockstep with
the pain001 suite on `feat/v<next-version>`, the only pull request into
`main`. A release:

1. Rename `## [Unreleased]` in `CHANGELOG.md` to `## [0.0.N] - <date>`,
   rewrite `.github/release-highlights.md`, and check
   `scripts/verify-release-versions 0.0.N`.
2. Run the release workflow by hand with `dry-run` on (the default) and
   read the notes and checksums it uploads.
3. After the release PR is merged, the maintainer pushes a signed,
   annotated tag `v0.0.N` whose message is `pain001-sdk-dotnet v0.0.N`.
4. `release.yml` verifies the tag against `KEYS.asc`, builds, tests,
   packs, writes the CycloneDX SBOM and `SHA256SUMS`, attests build
   provenance, creates the GitHub release titled
   `pain001-sdk-dotnet 0.0.N`, checks it, and publishes to NuGet.

### Settings and secrets the maintainer creates

None of these exist yet; the workflows are ready for them.

| What | Where | Why |
| :--- | :--- | :--- |
| Environment `release`, with required reviewer and tag rule `v*` | Repository settings, Environments | Gates the GitHub release and NuGet jobs |
| Secret `NUGET_USER` in `release` | Environment secrets | The nuget.org account name `NuGet/login` exchanges the OIDC token for |
| Trusted publishing policy for `Pain001.Sdk` | nuget.org, account, Trusted Publishing | Repository `sebastienrousseau/pain001-sdk-dotnet`, workflow `release.yml`, environment `release` |
| `Pain001.` package ID prefix | nuget.org prefix reservation | Pending; reserves the namespace |
| Pages source "GitHub Actions" | Repository settings, Pages | `docs.yml` deploys the manual (`build_type: workflow`) |
