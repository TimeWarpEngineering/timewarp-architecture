# Group on CliOperationBinding so a neutral contract can carry its CLI group

## Description

Roslynk task 003 (TimeWarpEngineering/Roslynk, fork issue #4) derived a Nuru CLI (`roslynk-cli`) from the 29
`[Operation]` contracts that Roslynk 002 put behind its MCP tools. The 29 commands shipped **ungrouped**:
Nuru 4.0.0-beta.2 takes an endpoint's `groupPath` only from a containing `[NuruRouteGroup]` or from
`[NuruCliBinding(Group = ...)]` on the request itself, and `TimeWarp.Contracts.CliOperationBinding` has only
`Pattern`. A contract-first engine therefore cannot say "this operation belongs to the `symbols` group"
without referencing TimeWarp.Nuru, which Roslynk deliberately keeps out of its engine.

Add a neutral way for a contract to carry its CLI group:

- `CliOperationBinding.Group` (string, optional; segments separated by `.` or space, matching how Nuru's
  `--group-filter` splits) in the catalog, and a declaration the contract author can write without Nuru
  (for example `[Operation("get_symbol", Group = "symbols")]` or a `[CliGroup("symbols")]` attribute in
  TimeWarp.Contracts). The operation catalog reports it.
- The group is **metadata only**: it sets `groupPath` and must not prefix the route pattern, so the CLI
  command stays the MCP tool name (`get_symbol`, not `symbols get_symbol`). Nuru (and 508-004's MCP tool
  filtering) read it the same way.

This is the contract-side alternative to **timewarp-nuru 508-012** (groups assigned from the app without
prefixing the route). Decide together: if 508-012 lands an app-side mapping that Roslynk's CLI can use,
this task may shrink to "not needed" or to the catalog field only.

Intended Roslynk groups (from 003): `symbols` (get_symbol, get_members, get_symbol_body, find_definition,
search_symbols, get_expression_info, get_type_hierarchy, find_implementations), `references`
(find_references, get_callers, get_callees, find_reads, find_writes, find_dead_code,
find_dead_conditionals), `solution` (open_solution, get_solution_status, reload_solution, get_diagnostics,
get_code_actions), `edit` (apply_code_action, apply_code_fix, extract_method, change_signature,
rename_symbol, rename_parameter, remove_unused_usings, apply_patch), `batch` (multi_query). A flat catalog
of 29 tools is about 16k tokens (timewarp-nuru 506), which is why groups matter.

## Checklist

- [ ] Decide with timewarp-nuru 508-012 which side owns the group (contract, app, or both)
- [ ] If contract-side: `Group` on `CliOperationBinding` plus an author-facing declaration in TimeWarp.Contracts
- [ ] Generator / catalog reports the group; docs say it never prefixes the route
- [ ] Nuru follow-up (or 508-012) consumes it as `groupPath`; Roslynk 003's groups can then be declared

## Session

- Created: 559053 (2026-10-19) by the Roslynk 003 implementer as that task's architecture follow-up.

## Notes

- Related: Roslynk 003 (CLI, ungrouped), timewarp-nuru 508-012 (app-side groups), 508-004 (McpHost reads
  the same group path for tool filtering), timewarp-architecture 384/387 (TimeWarp.Contracts), 386.
