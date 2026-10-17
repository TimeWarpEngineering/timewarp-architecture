# Roles slice, Operation info analyzer, skill and docs; promote TimeWarp.Contracts to 1.0.0

## Description

Step 11 of the serial order recorded on parent 384. Do not start until steps 3–10 are done
(Roslynk typed refactor and CLI, timewarp-nuru 508-001/002/003/004, 507 gate). Parent 384
covers step 2 only: `TimeWarp.Contracts` 1.0.0-beta.1.

Adopt `TimeWarp.Contracts` in the template's living example and tooling, then promote the
package from beta to 1.0.0 once it is proven (see Versioning on 384).

## Checklist

- [ ] Info analyzer suggests `[Operation]` on a hosted contract without one
- [ ] Roles slice (`web/features/admin/roles/`) annotated with `[Operation]` as the living example
- [ ] Contracts adopt `IQuery<...>` / `ICommand<...>` in place of bare `IRequest<...>` where the kind is clear; decide whether `IIdempotentCommand` maps to PUT/DELETE by convention
- [ ] `tw-web-api-contracts` skill describes the neutral layer and the three bindings (HTTP, CLI, MCP)
- [ ] Proven: Roslynk consumes it for CLI and MCP, roles slice consumes it, no breaking change needed
- [ ] Promote `TimeWarp.Contracts` to 1.0.0

## Session

- Created: 2022499 (2026-10-17), split from 384

## Notes

- Parent: 384. Serial order, versioning and package contents are recorded there.
