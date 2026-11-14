<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Architecture

`Pain001.Sdk` is a thin client: every call is a request to
`pain001 serve`, which does the work ([ADR 0001](adr/0001-thin-client-over-the-server.md)).
This page is for contributors: where things live and how a call flows.

## Layout

| Path | What it holds |
| :--- | :--- |
| `src/Pain001.Sdk/Pain001Client.cs` | The client: one public method per endpoint, the send path, timeouts and retries |
| `src/Pain001.Sdk/Pain001ClientOptions.cs`, `RetryPolicy.cs` | Configuration: base address, API key, timeout, retry policy, clock |
| `src/Pain001.Sdk/Models/` | Response records, mapped from the server's snake_case JSON |
| `src/Pain001.Sdk/Internal/Requests.cs` | Request bodies (`UiFilePayload`, `CheckRequest`, `FaceRequest`) |
| `src/Pain001.Sdk/Internal/Wire.cs` | JSON options, body parsing, error-detail extraction, decoding, refusal detection |
| `src/Pain001.Sdk/Pain001*Exception.cs` | The error model ([ADR 0003](adr/0003-error-model.md)) |
| `conformance/` | Core's conformance kit, vendored byte for byte |
| `tests/Pain001.Sdk.Tests/` | Unit tests, FsCheck properties, the kit runner, live-server tests |
| `examples/` | One console project per method |
| `benchmarks/` | BenchmarkDotNet: client-side and loopback |

## How a call flows

```text
GenerateAsync(content, filename, messageType, scheme)
  -> FilePayload: argument checks, a UiFilePayload record
  -> SendPayloadAsync: System.Text.Json to UTF-8 bytes (nulls omitted)
  -> SendCoreAsync: POST, so one attempt (GETs go through the RetryPolicy)
  -> SendOnceAsync: HttpRequestMessage (Accept, Bearer key, JSON body),
     a linked CancellationTokenSource for the timeout, read every byte
  -> Wire.ParseBody: JsonElement; empty is Undefined, non-JSON is a string
  -> Wire.AsRefusal: 422 + success false -> Pain001GenerationRefusedException
  -> Wire.Decode: non-2xx -> Pain001ApiException; 2xx -> the record
  -> no XML in a 2xx -> Pain001NoDocumentException
```

`RequestAsync` stops after `ParseBody`: it returns the status and body,
whatever the status. The conformance runner uses it, so the kit tests
the same send path as every typed method.

## Invariants

- **No payment data in messages.** Exception messages name the status
  and counts; the detail and findings stay in properties.
  `ResponsePropertyTests` checks this with generated error bodies.
- **Every body can be read.** A body that is not valid UTF-8 or not JSON
  becomes a JSON string of its text, so `Detail.GetString()` never
  throws.
- **One request per POST.** Only a `GET` is ever retried
  ([ADR 0004](adr/0004-opt-in-retries-for-get-only.md)).
- **The caller's `HttpClient` is theirs.** The client never changes its
  settings and disposes only an `HttpClient` it created.
- **No third-party runtime dependency**
  ([ADR 0002](adr/0002-no-third-party-runtime-dependencies.md)).

## Where the contract comes from

The server's OpenAPI document is vendored at
`conformance/openapi.json`; the records follow its schemas by hand. The
kit's cases (`conformance/cases.json`) pin statuses and fields. When core
changes the wire, it ships a new kit; this repository copies it in,
updates `SHA256SUMS`, and changes the client until every case passes.
