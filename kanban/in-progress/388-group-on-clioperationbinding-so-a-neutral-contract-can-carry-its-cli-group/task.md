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

## Goal (implementer restatement)

An operation declares its purpose group once, on `[Operation]`, and every transport reads that
same value from the catalog. `Group` is an optional string. Segments are separated by `.` or a
space. A null, empty, or whitespace group is ungrouped, using `IsNullOrWhiteSpace`, the same
blank rule the operation name already uses. The group describes what the operation is about. It
is not stored on `CliOperationBinding`, and it never prefixes the route pattern or the command
name, so the CLI command stays the operation name (`get_symbol`, not `symbols get_symbol`).

Acceptance:

- `OperationAttribute` has optional `Group`. Blank means ungrouped.
- `OperationDescriptor` carries the group. The contracts generator and every `[Operation]` reader
  report it, and neither one folds it into a route or a command name.
- The package readme and the Design regions say the group is purpose metadata for every
  transport and is not a route or command prefix.
- Jaribu covers a set group, an omitted group, a blank group, and catalog output.
- `dev build` is 0 warnings and 0 errors, the touched tests pass, `dev check-version` passes
  with `<Version>` and the platform pins in agreement, and `ganda repo audit` is clean.
- Follow-ups are filed for Nuru to read the catalog group as `groupPath` (with 508-004 tool
  filtering) and for Roslynk to declare its five groups once this package ships.

## Checklist

- [x] Optional `Group` on `OperationAttribute` in TimeWarp.Contracts; blank means ungrouped (same
      blank rule as the name, `IsNullOrWhiteSpace`)
- [x] Operation catalog DTO carries the group; the contracts generator and every `[Operation]` reader report it
- [x] readme / Design region: group is purpose metadata, read by all transports, never prefixes the route
- [x] Tests (Jaribu): grouped, ungrouped, and blank group; catalog output
- [x] `dev build` 0/0, tests, `dev check-version` (version + pins bumped), `ganda repo audit` clean
- [x] Follow-ups filed: Nuru reads catalog group as `groupPath` (with 508-004 tool filtering); Roslynk
      declares its five groups once the package ships

## Session

- Created: 559053 (2026-10-19) by the Roslynk 003 implementer as that task's architecture follow-up.
- 2026-10-19: the earlier TimeWarp.Nuru 4.0.0-beta.2 boy-scout bump on this branch was superseded by
  task 389, which landed the same bump on master; master was merged in and the conflict resolved to master.
- 2026-10-19: spec rewritten per Steve's decision (group on `[Operation]`, contract-only; 508-012 shelved).
- 2026-10-19: implementation. `Group` on `[Operation]` and `OperationDescriptor`. Readers report it and do not prefix routes. Follow-ups: timewarp-nuru 508-014, Roslynk 003-001.
- 2026-10-20: implement node re-ran the gates on 3f31c686e. `dev build` 0/0, foundation contracts tests 33/33, source generator tests 141/141, `dev check-version` exit 0 (2.0.0-beta.23 against beta.22), `ganda repo audit` exit 0. No product changes.

## Results

`OperationAttribute.Group` is optional purpose metadata. The setter stores null when the value is
null, empty, or whitespace (`IsNullOrWhiteSpace`), the same blank rule as the operation name.
Dotted and spaced segments are kept as written. `OperationDescriptor.Group` carries that value
into the catalog. JSON catalog output (camelCase, nulls omitted) includes `group` only when it
is set, beside `name` and the CLI `pattern`. The pattern and the name stay unprefixed
(`get_symbol`, not `symbols get_symbol`).

The contracts generator emits `public const string OperationGroup` only when the group is
non-blank. `RouteTemplate` and `GetRoute` are unchanged. Endpoint emit metadata reports
`OperationGroup` with the same blank rule. The FastEndpoint template does not read it, so HTTP
route emission is unchanged. `CliOperationBinding` stays the route pattern only.

Source version stays `2.0.0-beta.23`. Task 390 already moved `<Version>` and the platform pins
one prerelease ahead of published `2.0.0-beta.22`. This group ships in that unreleased version.
A second bump would skip `2.0.0-beta.23`. `dev check-version` lists `TimeWarp.Contracts` among
the packages at the source version.

Follow-ups, filed and published to those repos' to-do columns:

- timewarp-nuru 508-014: read `[Operation].Group` as `groupPath` without prefixing the route.
  McpHost (508-004) already filters on that path. 508-012 stays shelved.
- Roslynk 003-001: declare the five groups on the 29 operations once the `TimeWarp.Contracts`
  release that contains `Group` is restored. The engine stays free of TimeWarp.Nuru.

### How to validate

Smoke:

1. `./bin/dev build`
2. `cd tests/foundation/foundation-contracts-tests && dotnet test -c Release`
3. `cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release`
4. `./bin/dev check-version`
5. `ganda repo audit`

Expect:

- `./bin/dev build` ends with 0 warnings and 0 errors.
- Foundation contracts tests: 33 passed, 0 failed.
- Source generator tests: 141 passed, 0 failed. Grouped, omitted, and whitespace groups are
  covered, and `RouteTemplate` stays `api/Roles` when the group is `symbols`.
- `dev check-version` exits 0. Source is `2.0.0-beta.23`, NuGet is `2.0.0-beta.22`, one
  prerelease increment ahead, and it reports the version is safe to release. Platform pins in
  `Directory.Packages.props` are `2.0.0-beta.23`.
- `ganda repo audit` exits 0.

## Notes

- Related: Roslynk 003 (CLI, ungrouped), timewarp-nuru 508-004 (McpHost reads the same group for tool
  filtering), timewarp-nuru 508-012 (shelved app-side groups), timewarp-architecture 384/387 (TimeWarp.Contracts), 386.
