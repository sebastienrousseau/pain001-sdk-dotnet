<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# ADR 0004: Retries are opt-in and cover GET calls only

- Status: Accepted
- Date: 2026-11-14

## Context

A server behind a load balancer answers the odd 503 or 429 while an
instance restarts or sheds load. Retrying hides that, but a retried
`POST /api/v1/ui/generate` is a second payment file, and a client that
retries by default changes behaviour under callers who never asked.

## Decision

`Pain001ClientOptions.Retry` takes a `RetryPolicy`; it is `null` by
default, and then every request is sent once, as before. With a policy:

- only a `GET` is retried: `HealthAsync`, `FacesAsync` and
  `RequestAsync` with `GET`. A `POST` never is, whatever the policy;
- it retries HTTP 429 and 503, and `HttpRequestException` (a connection
  error); not other statuses, a timeout or a cancellation;
- up to `MaxAttempts` (default 3) attempts in all, waiting a jittered
  exponential backoff: half to all of
  `min(MaxDelay, InitialDelay × 2^(n-1))` (defaults 200 ms and 5 s);
- a `Retry-After` header (seconds or an HTTP date) replaces the backoff;
  one longer than `MaxDelay` returns the answer instead of waiting.

`Pain001ClientOptions.TimeProvider` is the clock it waits on, so tests
run without sleeping. The suite specifies the same policy for every
SDK, under each language's own name (Go `WithRetry`; Java, Rust and C++
`RetryPolicy`).

## Consequences

- Behaviour is unchanged for existing callers.
- A caller who needs payment calls retried makes that decision in their
  own code, with their own idempotency handling.
- `Timeout` stays per attempt: with retries, a call can take up to
  `MaxAttempts × Timeout` plus the waits.
