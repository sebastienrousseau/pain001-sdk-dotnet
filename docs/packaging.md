<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Packaging

## NuGet

| Item | Value |
| :--- | :--- |
| Package | [`Pain001.Sdk`](https://www.nuget.org/packages/Pain001.Sdk) (not yet published) |
| Target | `net10.0` |
| Licence | `Apache-2.0 OR MIT` (SPDX expression in the package) |
| Runtime dependencies | None |
| Symbols and docs | XML documentation in the package, symbols in a `.snupkg`; deterministic build |
| Publishing | `.github/workflows/release.yml`, on a signed `v<version>` tag, by NuGet trusted publishing (OIDC) from the `release` environment |
| Prefix | `Pain001.` reservation requested from nuget.org; pending |

Install it with:

```sh
dotnet add package Pain001.Sdk --version 0.0.88
```

Every release attaches, besides the `.nupkg` and `.snupkg`, a CycloneDX
SBOM, `SHA256SUMS` and a build provenance attestation. Verify a
downloaded package with:

```sh
sha256sum --check --ignore-missing SHA256SUMS
gh attestation verify Pain001.Sdk.0.0.88.nupkg --repo sebastienrousseau/pain001-sdk-dotnet
```

NuGet also signs every package it accepts with its repository
signature (`dotnet nuget verify Pain001.Sdk.0.0.88.nupkg`).

## Version pinning

The repository pins every package exactly in `Directory.Packages.props`
and locks the graph with content hashes in `packages.lock.json`; CI
restores in locked mode. A consumer depends on nothing transitively.

## Distribution packages

Not applicable. This is a library consumed through NuGet by .NET
projects; there is no binary or CLI to package for Debian, Fedora,
Homebrew or Nix, and .NET distribution packages carry the runtime and
SDK, not application libraries.

## Building from source offline

```sh
dotnet restore --locked-mode     # needs the packages in a local feed or cache
dotnet build -c Release
dotnet pack src/Pain001.Sdk/Pain001.Sdk.csproj -c Release -o artifacts
```

The library itself restores nothing from NuGet; only the test, coverage
and benchmark projects do.
