<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Contributing

This repository is the .NET client for the pain001 REST API. It is thin
on purpose: the server does the work, and this SDK turns its JSON into
typed .NET calls. Read [AGENTS.md](AGENTS.md) for the rules that must not
drift.

## Before you open a pull request

You need the .NET 10 SDK (it builds both the `net8.0` and `net10.0`
targets) and, to run the tests, the .NET 8 runtime as well.

```sh
make check          # format, build, unit tests, vulnerable-package audit
```

`make check` is what CI runs on every pull request. The build treats
every warning as an error, with the `latest-recommended` .NET analysers
and CA1502 holding every method to a cyclomatic complexity of 10.

### Against a live server

The conformance kit and the typed tests need a running server; they skip
without one.

```sh
uv venv /tmp/pain001-srv
VIRTUAL_ENV=/tmp/pain001-srv uv pip install 'pain001[api]==0.0.85'
/tmp/pain001-srv/bin/pain001 serve --host 127.0.0.1 --port 8000 &
PAIN001_BASE_URL=http://127.0.0.1:8000 make conformance
```

## What the tests are for

- `ClientTests`: the client against an in-memory `HttpMessageHandler`:
  paths, headers, JSON bodies, error mapping, timeouts.
- `RunnerTests`: the conformance runner's JSON Pointer, `$file` and
  failure reporting, without a server.
- `KitTests`: the vendored kit matches `conformance/SHA256SUMS`.
- `ConformanceTests`: every case in `conformance/cases.json` against a
  live server.
- `TypedServerTests`: one test per typed method against a live server.

## The conformance kit

`conformance/` is vendored from [core](https://github.com/sebastienrousseau/pain001),
byte for byte. Never edit it here: change it in core, then copy the new
kit in and regenerate `conformance/SHA256SUMS`:

```sh
cd conformance && find . -type f ! -name SHA256SUMS | sed 's|^\./||' \
  | LC_ALL=C sort | xargs shasum -a 256 > SHA256SUMS
```

## Commits

Conventional commits (`feat:`, `fix:`, `docs:`, ...), with a body that
says why. Every commit is signed off (`git commit -s`), certifying the
[Developer Certificate of Origin](DCO.txt).

## Versioning

The version is `0.0.x`, moves by `0.0.1`, and ships in lockstep with the
pain001 suite. It is stated in `src/Pain001.Sdk/Pain001.Sdk.csproj` and
`CITATION.cff`, with a heading in `CHANGELOG.md`.

## Licence

Apache-2.0 OR MIT, at your option. By contributing you agree your work is
released under the same dual grant.
