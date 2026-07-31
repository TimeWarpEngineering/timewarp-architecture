# Add MTP support and Jaribu family aggregators to dev test

## Description

Make co-located Jaribu tests reachable by `dev test` and CI. **Decided (Steve, 2026-07-29,
task 134 findings §8 Q2):** per-family **aggregator projects under `tests/`** are the CI
entry — `dev test` stays csproj-based; standalone `./file.cs` runs remain the local dev loop
and need no tooling. This task also fixes spike blocker M2: `dev test`'s current
`dotnet test <csproj-path> -c Release` invocation fails against Microsoft.Testing.Platform
projects on .NET 10 ("Testing with VSTest target is no longer supported").

**Depends on task 135** (co-located `-tests.cs` files must exist on dev with the
template-safe JARIBU_MULTI switch before aggregators can glob them). Do not start before 135
is done. Evidence base: `kanban/done/134-spike-jaribu-co-located-integration-tests/`
(findings.md §4 blocker 2; review/round-1/general.md issue 2; spike aggregator
`tests/container-apps/jaribu-spike-tests/` on the spike branch as reference — port fresh,
don't merge).

## Requirements

1. **`dev test` MTP invocation (tools/dev-cli/endpoints/test-command.cs):** detect MTP test
   projects (e.g. `TestingPlatformDotnetTestSupport=true` or a project-local `global.json`
   `test.runner` opt-in) and invoke them in a supported form. Spike evidence: bare
   `dotnet test` from inside the project directory works (7/7 in 605ms); both
   `dotnet test <csproj-path>` and `--project <path>` fail on .NET 10. MTP projects are also
   plain executables — `dotnet run --project` is a candidate; implementer picks the mechanism
   and proves it. Existing Fixie projects must keep working unchanged (mixed suite during
   migration); serialized execution preserved (fixed ports).
2. **Per-family aggregator projects** under `tests/container-apps/<family>/<family>-jaribu-tests/`
   (web + api now; grpc when it gains co-located tests): `JARIBU_MULTI` defined,
   `TimeWarp.Jaribu.TestingPlatform` referenced (CPM pin exists since the spike... verify it
   landed with 135, else add), `Compile Include` + `Link` globbing that family's
   `features/**/*-tests.cs` and `platform/**/*-tests.cs`, plus the `#:project` equivalents as
   ProjectReferences (timewarp-testing, contracts, etc.).
3. **global.json handling:** the MTP `test.runner` opt-in cannot live in a csproj. Any
   project-local `global.json` MUST mirror the root sdk pin (spike landmine — an unpinned
   local global.json silently switched SDKs; timewarp-jaribu#20). Evaluate whether the root
   `global.json` can carry `test.runner` for the whole repo without breaking the remaining
   Fixie projects' `dotnet test` path; choose the least-duplication arrangement that keeps
   both frameworks green.
4. **Template implications:** the `tests/` tree ships in template output. Aggregator projects
   must be template-safe and conditioned on their family's flag (`api`, `web`) like sibling
   test projects; `dev template-smoke` must stay green and should assert a generated app's
   aggregator builds and runs (complements 135's regression coverage).
5. **Gates:** full `dev test` run green (Fixie projects + new aggregators, serialized);
   `dev build` 0/0; `dev template-smoke` green; `ganda repo audit` clean. CI
   (`workflow.yml` / `dev workflow`) needs no semantic change — confirm, don't assume.

Out of scope: migrating Fixie suites; a direct-runfile mode for `dev test` (explicitly
rejected in Q2 — aggregators are the only CI entry); Aspire testing tier (134 findings §8 Q3,
still open).

## Checklist

- [ ] `dev test` invokes MTP projects in a supported form; Fixie projects unaffected
- [ ] web + api `<family>-jaribu-tests` aggregators globbing co-located `-tests.cs`
- [ ] `dotnet test` discovery verified per aggregator (counts match co-located files)
- [ ] global.json arrangement chosen; sdk pins mirrored; both frameworks green
- [ ] Aggregators template-flag-conditioned; template-smoke asserts generated-app aggregator
- [ ] Full `dev test` green serialized; `dev build` 0/0; `ganda repo audit` clean
- [ ] CI workflow semantics confirmed unchanged
- [ ] Kanban mutations committed

## Notes

- Decision trail: 134 findings §8 — Q1 registered-unrouted `tests` suffix (task 135),
  Q2 family aggregators (this task), Q3 Aspire tier still open.
- Stale-`dev`-binary footgun: this task edits dev-cli — verify changed dev-cli code via
  runfile or self-install before trusting `./bin/dev` output.
- **Implementation plan:** `plan.md` (2026-07-31). Locked: D1 bare `dotnet test` cwd=project;
  D2 per-aggregator global.json (no root runner); D3 pin TestingPlatform beta.14; D4 not in
  .slnx; D5 web→contracts, api→contracts+timewarp-testing; D6 template path excludes; D7
  template-smoke tier 3 (web 5 / api 2); D8 CI unchanged. Order: MTP invoke **before** any
  aggregator csproj appears.

## Session

- Created: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-29)
- Plan: 2026-07-31 (orchestration tw-orchestrate-task 136)
