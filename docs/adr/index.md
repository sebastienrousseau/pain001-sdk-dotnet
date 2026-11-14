<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Architecture decision records

Decisions that will be questioned later, with the reasons they were
made. A new ADR is merged with the change it explains; a superseded one
stays, marked as such, with a link to what replaced it.

| ADR | Decision | Status |
| :--- | :--- | :--- |
| [0001](0001-thin-client-over-the-server.md) | A thin client over `pain001 serve`, proven by core's conformance kit | Accepted |
| [0002](0002-no-third-party-runtime-dependencies.md) | No third-party runtime dependency | Accepted |
| [0003](0003-error-model.md) | Exceptions by cause; messages never carry payment data | Accepted |
| [0004](0004-opt-in-retries-for-get-only.md) | Retries are opt-in and cover `GET` calls only | Accepted |
| [0005](0005-target-net10-only.md) | Target `net10.0` only | Accepted |
