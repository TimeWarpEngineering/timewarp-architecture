# Roles slice, Operation info analyzer, skill and docs for TimeWarp.Contracts

## Description

Originally step 11 of the cross-repo serial order recorded on 384. That gate is lifted
(Steve, 2026-10-20): this repo takes no code dependency on Roslynk or timewarp-nuru, and the
contract shape is settled by 384, 387 and 388 (`TimeWarp.Contracts` 2.0.0-beta.23 on nuget.org,
including the optional `Group` on `[Operation]`). Changes forced later by Roslynk or Nuru are
ordinary follow-up tasks here.

Adopt `TimeWarp.Contracts` in the template's living example and tooling. The package versions
with the repo (decided 2026-10-18 on 384), so there is no separate promotion step here.

## Depends on

- 384

## Checklist

- [ ] Info analyzer suggests `[Operation]` on a hosted contract without one
- [ ] Roles slice (`web/features/admin/roles/`) annotated with `[Operation]` as the living example
- [ ] Contracts adopt `IQuery<...>` / `ICommand<...>` in place of bare `IRequest<...>` where the kind is clear; decide whether `IIdempotentCommand` maps to PUT/DELETE by convention
- [ ] `tw-web-api-contracts` skill describes the neutral layer and the three bindings (HTTP, CLI, MCP)
- [ ] Proven: the roles slice consumes it with no breaking change needed (outside proof for CLI and MCP
      comes from Roslynk 002 and 003-001, not from this task)

## Session

- Created: 2022499 (2026-10-17) as child 384-001, split from 384
- Re-filed standalone as 386 so 384 can close: 2560486 (2026-10-18)
- 2026-10-20: serial-order gate lifted per Steve; Roslynk proof item moved out of scope.

## Notes

- Follows 384 (not a child). Serial order, versioning and package contents are recorded there.
