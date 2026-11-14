<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# ADR 0003: Exceptions by cause; messages never carry payment data

- Status: Accepted
- Date: 2026-11-14

## Context

Request bodies hold payment data (names, IBANs, amounts), and the server
can echo them in an error's `detail`. Exception messages end up in logs.
pain001 0.0.87 also changed how generate refuses data: HTTP 422 with the
generate response as the body, instead of HTTP 200 with
`success: false`. The suite's SDKs agreed on one shape for this
(Go `RefusalError` and `ErrNoDocument`, Java `GenerationRefusedException`,
C++ `GenerationRefused` and `NoDocument`).

## Decision

A typed call raises, by cause:

| Exception | When | Carries |
| :--- | :--- | :--- |
| `Pain001GenerationRefusedException` (a `Pain001ApiException`) | Generate answered 422 with `success: false` | `StatusCode`, `Detail` (the body), `Response` (the decoded findings) |
| `Pain001ApiException` | Any other status outside 2xx | `StatusCode`, `Detail` (the `detail` field, or the body) |
| `Pain001NoDocumentException` | Generate answered 2xx without the XML | `StatusCode`, `Response` |
| `JsonException` | A 2xx body that is not the expected shape | |
| `TimeoutException` | `Pain001ClientOptions.Timeout` passed | |
| `OperationCanceledException` | The caller's token was cancelled | |
| `HttpRequestException` | The request could not be sent | |

Messages name the status and, for a refusal, the count of each kind of
finding; never the detail or a finding. `RequestAsync` raises none of
the HTTP ones: it returns the status and body.

## Consequences

- `catch (Pain001ApiException)` keeps catching a refusal; a caller that
  wants the findings catches the subclass first.
- A returned `UiGenerateResponse` always has `Success` true and the XML.
- An FsCheck property (`ResponsePropertyTests`) checks that nothing
  else escapes a typed method, for any status, body and media type.
