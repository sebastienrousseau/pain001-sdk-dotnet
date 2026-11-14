<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Benchmarks

Two questions, two benchmark classes in
`benchmarks/Pain001.Sdk.Benchmarks` (BenchmarkDotNet):

- **Client side** (`ClientSideBenchmarks`): what the SDK itself costs
  per call, with no network. Each method builds and serializes its
  request from the conformance kit's fixtures (the five-payment
  credit-transfer CSV, its pain.001.001.09 document) and decodes a
  recorded answer from `pain001 serve` 0.0.87 to those fixtures,
  through an in-memory `HttpMessageHandler`.
- **Loopback** (`LoopbackBenchmarks`): one full round trip per method to
  a real `pain001 serve` on 127.0.0.1, with the same fixtures. This is
  the client, the loopback TCP stack and the server's own work
  together. It is skipped, with a message, when `PAIN001_BASE_URL` is
  unset.

## Method

```sh
make build
PAIN001_BASE_URL=http://127.0.0.1:8000 make bench   # full run, results in artifacts/benchmarks/
make bench-smoke                                    # one iteration each, as CI runs it
```

BenchmarkDotNet's default job, in process (`--inProcess`), with
`[MemoryDiagnoser]`. The numbers below are one run on one machine; they
show orders of magnitude, not a guarantee. CI runs the smoke job on
every pull request to keep the benchmarks compiling and running; it
asserts nothing.

## Environment

| Item | Value |
| :--- | :--- |
| Machine | Mac with an Apple A18 Pro (6 cores), 8 GB |
| OS | macOS 27.2 (26B123) |
| .NET | SDK 10.0.404, runtime 10.0.15, Arm64 RyuJIT |
| BenchmarkDotNet | 0.15.8, `InProcessEmitToolchain` |
| Server | `pain001 serve` 0.0.87 (Python 3.12, uvicorn), same machine, 127.0.0.1 |
| Date | 2026-11-14 |

The machine was not otherwise idle during the loopback run, and the
server shares its CPU with the benchmark, so the loopback numbers have
wide error bars.

## Results: client side (no network)

| Method | Mean | Error | StdDev | Median | Allocated |
| :--- | ---: | ---: | ---: | ---: | ---: |
| Health | 1.899 μs | 0.060 μs | 0.164 μs | 1.875 μs | 2.83 KB |
| Validate | 6.380 μs | 0.408 μs | 1.151 μs | 6.085 μs | 8.95 KB |
| Generate | 30.942 μs | 1.789 μs | 4.986 μs | 29.578 μs | 28.66 KB |
| Check | 26.164 μs | 1.361 μs | 3.772 μs | 25.153 μs | 23.17 KB |
| Faces | 4.735 μs | 0.223 μs | 0.614 μs | 4.575 μs | 5.84 KB |
| ProjectFace | 50.056 μs | 3.067 μs | 8.498 μs | 47.086 μs | 49.25 KB |
| RawRequest | 1.577 μs | 0.092 μs | 0.254 μs | 1.481 μs | 2.53 KB |

## Results: loopback round trip

| Method | Mean | Error | StdDev | Median | Allocated |
| :--- | ---: | ---: | ---: | ---: | ---: |
| Health | 3.939 ms | 1.334 ms | 3.742 ms | 2.518 ms | 4.89 KB |
| Validate | 11.757 ms | 1.758 ms | 4.813 ms | 9.991 ms | 7.66 KB |
| Generate | 55.645 ms | 12.233 ms | 35.295 ms | 49.719 ms | 27.44 KB |
| Check | 18.644 ms | 3.390 ms | 9.561 ms | 15.559 ms | 12.6 KB |
| Faces | 7.200 ms | 1.703 ms | 4.832 ms | 5.238 ms | 7.92 KB |
| ProjectFace | 35.881 ms | 9.547 ms | 27.392 ms | 29.745 ms | 38.69 KB |
| RawRequest | 1.191 ms | 0.138 ms | 0.386 ms | 1.092 ms | 4.55 KB |

## Reading them

- The client's own cost is microseconds: at most about 50 μs (a face
  projection, whose answer is the largest body) against milliseconds
  for the round trip. Its share of a loopback call is under 0.2 % for
  every method here; on a real network it is smaller still.
- The round trip is the server's work: generation and projection, which
  build and validate a document, are the slowest.
- `Health` and `RawRequest` send the same `GET /api/v1/health`; their
  loopback means differ because of the noise noted above, and their
  medians are within 1.5 ms of each other.
