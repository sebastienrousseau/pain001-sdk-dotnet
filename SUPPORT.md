<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Getting support

## Read first

- [README.md](README.md): install, quick start, configuration and the
  example index.
- The [user manual](https://sebastienrousseau.github.io/pain001-sdk-dotnet/),
  built from [`docs/`](docs/index.md), with the API reference generated
  from the XML documentation comments.
- [`examples/`](examples/): one runnable project per client method,
  each run against `pain001 serve` in CI.
- [DEVELOPMENT.md](DEVELOPMENT.md): building, testing and every CI
  gate on your own machine.

## Questions and how-to

Open a [GitHub issue](https://github.com/sebastienrousseau/pain001-sdk-dotnet/issues/new/choose)
(GitHub Discussions are not enabled). Include:

- the SDK version (`Pain001.Sdk` package or commit) and the .NET SDK
  version (`dotnet --version`);
- the pain001 server version (`GET /api/v1/health`, or
  `pain001 --version`);
- the smallest synthetic input and code that show the problem;
- the exception type, `StatusCode` and message.

Never paste real account numbers or personal data. An exception's
`Detail` or `Response` can quote the request: redact it first.

A question about what the server accepts or produces (a validation
rule, a scheme profile, the XML) belongs in
[pain001](https://github.com/sebastienrousseau/pain001/issues): this SDK
only carries the request and the answer.

## Bugs and feature requests

Use the [bug report](https://github.com/sebastienrousseau/pain001-sdk-dotnet/issues/new?template=bug_report.yml)
or [feature request](https://github.com/sebastienrousseau/pain001-sdk-dotnet/issues/new?template=feature_request.yml)
form. Security reports never go in an issue: follow
[SECURITY.md](SECURITY.md).

## Supported versions

The SDK ships in lockstep with the pain001 suite. While pre-`1.0`, only
the latest `0.0.x` receives fixes.

| Version | Status | Supported |
| :--- | :--- | :--- |
| `0.0.88` | Unreleased, in development | Yes, once released |

## Response times

This project has a single maintainer (see [GOVERNANCE.md](GOVERNANCE.md)).
Issues are usually answered within a few business days; security
reports follow the timeline in [SECURITY.md](SECURITY.md).
