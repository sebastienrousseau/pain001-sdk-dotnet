<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Governance

`pain001-sdk-dotnet` is one repository of the pain001 suite, released
in lockstep with [pain001](https://github.com/sebastienrousseau/pain001)
(the server) and its other SDKs.

## Roles

| Role | Who | Responsibilities |
| :--- | :--- | :--- |
| Maintainer | Sebastien Rousseau ([@sebastienrousseau](https://github.com/sebastienrousseau)) | Reviews and merges pull requests, decides when a release opens, signs tags, publishes the NuGet package, answers security reports, owns the repository settings and secrets |
| Contributor | Anyone | Opens issues and pull requests under [CONTRIBUTING.md](CONTRIBUTING.md), signing off every commit ([DCO.txt](DCO.txt)) |
| Automated contributor | AI coding agents | Work under [AGENTS.md](AGENTS.md): prepare commits and pull requests, never merge, tag or publish |

This is a single-maintainer project. There is no committee and no vote:
the maintainer decides, in the open, on the pull request or issue
concerned.

## How decisions are made

- **Day to day**: on the pull request. CI ([DEVELOPMENT.md](DEVELOPMENT.md))
  is the bar every change meets; review checks what CI cannot.
- **Decisions that will be questioned later** get an Architecture
  Decision Record in [`docs/adr/`](docs/adr/index.md), merged with the change it
  explains.
- **The wire contract** belongs to the server. A change to what the
  server accepts or answers is decided in pain001 and reaches this SDK
  through the vendored conformance kit; this repository does not
  diverge from it.
- **Versions** move by `0.0.1`, with the suite; the maintainer opens a
  release with `feat/v<next-version>`.

## Access continuity

The project must survive the maintainer becoming unavailable.

- **Source**: the Git history is public and mirrored by every clone.
  The licence (Apache-2.0 OR MIT) lets anyone fork and continue it.
- **Releases**: tags are signed with the key in [KEYS.asc](KEYS.asc);
  a successor publishes a new key there, in a signed commit, before
  signing a tag with it.
- **NuGet**: the package is published by trusted publishing (OIDC) from
  the `release` GitHub environment, so no long-lived API key exists to
  lose. The nuget.org package owners list is the access point: the
  maintainer adds a second owner before handing over.
- **GitHub**: repository admin rights follow the maintainer's GitHub
  account. A successor is added as an admin of
  `sebastienrousseau/pain001-sdk-dotnet` before any handover, together
  with the pain001 repository, which owns the conformance kit.
- **Security reports**: GitHub private vulnerability reporting stays on,
  so reports reach whoever holds admin rights. The email channel in
  [SECURITY.md](SECURITY.md) moves with the pain001.com domain.

## Changing this document

Changes to governance go through a pull request like any other change,
and are recorded in the [CHANGELOG](CHANGELOG.md).
