# Operation group on [Operation] so a contract declares its group once

## Description

Roslynk task 003 (TimeWarpEngineering/Roslynk, fork issue #4) derived a Nuru CLI (`roslynk-cli`) from the 29
`[Operation]` contracts that Roslynk 002 put behind its MCP tools. The 29 commands shipped **ungrouped**:
Nuru 4.0.0-beta.2 takes an endpoint's `groupPath` only from a containing `[NuruRouteGroup]` or from
`[NuruCliBinding(Group = ...)]` on the request itself, and TimeWarp.Contracts has no group. A contract-first
engine therefore cannot say "this operation belongs to the `symbols` group" without referencing
TimeWarp.Nuru, which Roslynk deliberately keeps out of its engine.

Add an optional, transport-neutral group to the operation contract:

- `[Operation("get_symbol", Group = "symbols")]`: `Group` is a string; segments are separated by `.` or
  space, matching how Nuru's `--group-filter` splits. It lives on `[Operation]`, **not** on
  `CliOperationBinding`: the group says what the operation is about (its purpose), so every transport uses
  it, not only the CLI.
- The operation catalog reports the group once per operation. Hosts read it from the catalog:
  - CLI (Nuru): `groupPath`
  - MCP host (timewarp-nuru 508-004): tool filtering
  - HTTP: OpenAPI tag (optional follow-up, not required here)
- The group is **metadata only**. It never prefixes the route pattern or the command name: the CLI command
  stays the MCP tool name (`get_symbol`, not `symbols get_symbol`).

## Decision (Steve, 2026-10-19)

The group is declared in the contract and nowhere else. TimeWarp is the primary consumer of its own
contracts (everything ships under the Unlicense), so we control the contracts. An app-side mapping keyed by
operation name would be a second source of truth: a new operation would silently ship ungrouped and a
renamed one would leave a dead entry, with no build failure. An app-side override for contracts we do not
control is deferred until a real consumer needs it; it can be layered on later without breaking the
contract field. timewarp-nuru 508-012 (app-side groups) is shelved to backlog for that reason and is not a
prerequisite.

Intended Roslynk groups (from 003): `symbols` (get_symbol, get_members, get_symbol_body, find_definition,
search_symbols, get_expression_info, get_type_hierarchy, find_implementations), `references`
(find_references, get_callers, get_callees, find_reads, find_writes, find_dead_code,
find_dead_conditionals), `solution` (open_solution, get_solution_status, reload_solution, get_diagnostics,
get_code_actions), `edit` (apply_code_action, apply_code_fix, extract_method, change_signature,
rename_symbol, rename_parameter, remove_unused_usings, apply_patch), `batch` (multi_query). A flat catalog
of 29 tools is about 16k tokens (timewarp-nuru 506), which is why groups matter.

## Checklist

- [ ] Optional `Group` on `OperationAttribute` in TimeWarp.Contracts; blank means ungrouped (same
      blank rule as the name, `IsNullOrWhiteSpace`)
- [ ] Operation catalog DTO carries the group; the contracts generator and every `[Operation]` reader report it
- [ ] readme / Design region: group is purpose metadata, read by all transports, never prefixes the route
- [ ] Tests (Jaribu): grouped, ungrouped, and blank group; catalog output
- [ ] `dev build` 0/0, tests, `dev check-version` (version + pins bumped), `ganda repo audit` clean
- [ ] Follow-ups filed: Nuru reads catalog group as `groupPath` (with 508-004 tool filtering); Roslynk
      declares its five groups once the package ships

## Session

- Created: 559053 (2026-10-19) by the Roslynk 003 implementer as that task's architecture follow-up.
- 2026-10-19: the earlier TimeWarp.Nuru 4.0.0-beta.2 boy-scout bump on this branch was superseded by
  task 389, which landed the same bump on master; master was merged in and the conflict resolved to master.
- 2026-10-19: spec rewritten per Steve's decision (group on `[Operation]`, contract-only; 508-012 shelved).

## Notes

- Related: Roslynk 003 (CLI, ungrouped), timewarp-nuru 508-004 (McpHost reads the same group for tool
  filtering), timewarp-nuru 508-012 (shelved app-side groups), timewarp-architecture 384/387 (TimeWarp.Contracts), 386.
