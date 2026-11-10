<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Security Policy

## Supported versions

This SDK follows the [`pain001`](https://github.com/sebastienrousseau/pain001)
suite cadence. While pre-`1.0`, the latest released `0.0.x` receives
security fixes; older `0.0.x` versions do not. No version has been
released yet: the first will be `0.0.86`, in lockstep with the suite.

| Version | Status | Receives security fixes? |
| :--- | :--- | :--- |
| `0.0.86` | Unreleased, in development | Yes, once released |

## Reporting a vulnerability

**Do not open a public issue for security reports.**

Use one of these private channels, in this order of preference:

1. **GitHub Private Vulnerability Reporting**:
   <https://github.com/sebastienrousseau/pain001-sdk-dotnet/security/advisories/new>
2. **Email**: `security@pain001.com`

Include the SDK version, the pain001 server version, and the smallest
synthetic input that shows the problem. Never send real account numbers
or personal data.

## Security posture

### Scope

This package is an HTTP client for the pain001 REST API. It sends JSON
to a server you configure and parses the JSON that comes back. It does
not parse XML, write files or run code from the server.

### How it handles risk

| Surface | How it's handled |
| :--- | :--- |
| **Payment data in logs** | The SDK logs nothing. Request bodies are never written anywhere, and an API error's message names only the HTTP status: the server's `detail`, which can echo input, stays in `Pain001ApiException.Detail`. |
| **API key** | Sent only as `Authorization: Bearer <key>` to the configured base address. Use `https` for any server that is not on loopback. |
| **Hostile or broken server** | Responses are parsed with `System.Text.Json`; a non-JSON body becomes a string, never an exception from inside the parser. Every request has a timeout (30 seconds by default). |
| **Retries** | None. A payment request is never sent twice by the SDK. |
| **Dependencies** | No third-party runtime dependency: only the .NET base class library. Test packages are pinned exactly in `Directory.Packages.props`, and CI fails on any package `dotnet list package --vulnerable` reports. |

### Cryptography status

This package implements no cryptography. Transport security is the
platform's `HttpClient` and TLS stack.
