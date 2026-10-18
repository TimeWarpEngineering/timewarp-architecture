# Roles slice, Operation info analyzer, skill and docs for TimeWarp.Contracts

## Description

Step 11 of the serial order recorded on 384. Do not start until steps 3–10 are done
(Roslynk typed refactor and CLI, timewarp-nuru 508-001/002/003/004, 507 gate). Task 384
covers step 2 only: `TimeWarp.Contracts` at the repo version (2.0.0-beta.20).

Adopt `TimeWarp.Contracts` in the template's living example and tooling. The package versions
with the repo (decided 2026-10-18 on 384), so there is no separate promotion step here.

## Depends on

- 384

## Checklist

- [ ] Info analyzer suggests `[Operation]` on a hosted contract without one
- [ ] Roles slice (`web/features/admin/roles/`) annotated with `[Operation]` as the living example
- [ ] Contracts adopt `IQuery<...>` / `ICommand<...>` in place of bare `IRequest<...>` where the kind is clear; decide whether `IIdempotentCommand` maps to PUT/DELETE by convention
- [ ] `tw-web-api-contracts` skill describes the neutral layer and the three bindings (HTTP, CLI, MCP)
- [ ] Proven: Roslynk consumes it for CLI and MCP, roles slice consumes it, no breaking change needed

## Session

- Created: 2022499 (2026-10-17) as child 384-001, split from 384
- Re-filed standalone as 386 so 384 can close: 2560486 (2026-10-18)

## Notes

- Follows 384 (not a child). Serial order, versioning and package contents are recorded there.
