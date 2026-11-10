<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# AGENTS.md

`pain001-sdk-dotnet` is the typed .NET client for the pain001 REST API
(`pain001 serve`). The server is the one engine; this SDK is a thin
client that every case of the shared conformance kit proves. These are
the rules an automated change is most likely to break; humans follow
them too. Read [CONTRIBUTING.md](CONTRIBUTING.md) first.

## Versioning

- The version is `0.0.x`, moves by `0.0.1`, and ships in lockstep with
  the pain001 suite. Never bump it as a side effect of a change.
- It is stated in `src/Pain001.Sdk/Pain001.Sdk.csproj` (`<Version>`)
  and `CITATION.cff`, with a `## [0.0.N]` heading in `CHANGELOG.md`.
- SDK version 0.0.N vendors the conformance kit from core 0.0.N.
- Work for the next version happens on `feat/v<next-version>`, the only
  pull request into `main`.

## The gate

```sh
make check
```

It runs `dotnet format --verify-no-changes`, `dotnet build -c Release`
(warnings as errors, `latest-recommended` analysers, CA1502 cyclomatic
complexity at 10), `dotnet test` and the vulnerable-package audit. CI
adds the conformance job against a live server. "It compiles" is not
done.

## Rules

- No third-party runtime dependency: `HttpClient` and
  `System.Text.Json` only. Test package versions are pinned exactly in
  `Directory.Packages.props`.
- Never log or put a request body in an exception message: it holds
  payment data.
- No retries by default.
- Every source file opens with the SPDX header `Apache-2.0 OR MIT`.
- Synthetic data only: no real account numbers or personal data.

## Leave alone

- `conformance/` is vendored from core, byte for byte, and pinned by
  `conformance/SHA256SUMS`. Never edit it here; change it in core and
  copy it in.
