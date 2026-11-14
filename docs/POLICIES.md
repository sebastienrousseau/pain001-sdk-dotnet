<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Policies

## Minimum toolchain

| Item | Floor | Enforced by |
| :--- | :--- | :--- |
| Target framework | `net10.0` | `<TargetFramework>` in every project |
| SDK to build | .NET 10 SDK | `actions/setup-dotnet` with `10.0.x` in every workflow |
| Platforms tested | Linux, Windows and macOS, on the GitHub-hosted `*-latest` runners | `ci.yml` build matrix |

The floor is the oldest .NET release Microsoft supports. It moves:

- only when the current floor reaches end of support, or a newer LTS
  release is needed for a feature the client uses;
- in a release whose CHANGELOG says so under **Changed**, with an ADR
  ([ADR 0005](adr/0005-target-net10-only.md) records the current one);
- never as a side effect of another change.

No distribution packages are claimed ([packaging](packaging.md)), so
there is no distribution toolchain table to keep.

## Versioning

The version is `0.0.x` and moves by `0.0.1`, in lockstep with the
pain001 suite: SDK 0.0.N ships with core 0.0.N and runs the
conformance kit core ships. Before `1.0`, any release may change the
public API; the CHANGELOG says how, and the API compatibility check
(`scripts/api-compat.sh`) flags every break against the last published
package so none is accidental.

## Output stability

The SDK produces no payment output of its own: the XML comes from the
server. What the SDK guarantees is the mapping: the same server answer
decodes to the same records and raises the same exception types
([ADR 0003](adr/0003-error-model.md)). A change to that mapping is
breaking even when no signature moves.

## Deprecation

A public member is marked `[Obsolete]` with the replacement named, for
at least one release, before it is removed.
