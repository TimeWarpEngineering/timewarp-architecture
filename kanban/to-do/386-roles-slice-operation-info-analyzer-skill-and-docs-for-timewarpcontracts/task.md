# Roles slice, Operation info analyzer, skill and docs for TimeWarp.Contracts

## Description

Originally step 11 of the cross-repo serial order recorded on 384. That gate is lifted
(Steve, 2026-10-20): this repo takes no code dependency on Roslynk or timewarp-nuru, and the
contract shape is settled by 384, 387 and 388 (`TimeWarp.Contracts` 2.0.0-beta.23 on nuget.org,
including the optional `Group` on `[Operation]`). Changes forced later by Roslynk or Nuru are
ordinary follow-up tasks here.

Adopt `TimeWarp.Contracts` in the template's living example and tooling. The package versions
with the repo (decided 2026-10-18 on 384), so there is no separate promotion step here.

## Goal (implementer restatement)

Make the template's living example use `TimeWarp.Contracts` the way a generated app should.

- An info-level convention analyzer reports a hosted contract whose request has no
  `[Operation]`. Hosted means an `[ApiEndpoint]` outer type with a nested `Query` or
  `Command` that has `[ApiRoute]` and no `[ClientOnlyContract]`. It is info, not a
  warning, so the build stays clean while the rest of the template has no annotations.
  A later task can raise the severity.
- Each of the six roles contracts gets `[Operation("roles.<verb>", Description = ...)]`.
  The generator then copies the name and description into the endpoint metadata.
- Contracts whose kind is clear move from bare `IRequest<OneOf<...>>` to `IQuery<...>`
  or `ICommand<...>`, the roles slice uses `IIdempotentCommand<...>` for its retry-safe
  PUT and DELETE, and the PUT/DELETE convention question gets a written answer.
  Behavior does not change: `IQuery`, `ICommand`, and `IIdempotentCommand` all extend
  `IRequest`. Contracts whose kind is not clear stay on `IRequest` with a stated reason.
- `tw-web-api-contracts` describes the neutral layer (`[Operation]` plus the kind from the
  mediator interface) and the three bindings: HTTP from `[ApiRoute]`, CLI, and MCP.
  `documentation/` is retired, so the skill is the only place this is written.
- Proof: the roles slice builds and its tests pass on the published package shape, with no
  change to `TimeWarp.Contracts`. CLI and MCP proof is out of scope (Roslynk).

Acceptance: `dev build` has 0 warnings and 0 errors. The analyzer tests cover a report,
no report when `[Operation]` is present, and no report for client-only or unrouted
contracts. The source generator and roles tests pass, `ganda repo audit` passes, and
`dev template-smoke` succeeds.

## Depends on

- 384

## Checklist

- [x] Info analyzer suggests `[Operation]` on a hosted contract without one (TWA0035)
- [x] Roles slice (`web/features/admin/roles/`) annotated with `[Operation]` as the living example
- [x] Contracts adopt `IQuery<...>` / `ICommand<...>` in place of bare `IRequest<...>` where the kind is clear; decide whether `IIdempotentCommand` maps to PUT/DELETE by convention
- [x] `tw-web-api-contracts` skill describes the neutral layer and the three bindings (HTTP, CLI, MCP)
- [x] Proven: the roles slice consumes it with no breaking change needed (outside proof for CLI and MCP
      comes from Roslynk 002 and 003-001, not from this task)

## Session

- Created: 2022499 (2026-10-17) as child 384-001, split from 384
- Re-filed standalone as 386 so 384 can close: 2560486 (2026-10-18)
- 2026-10-20: serial-order gate lifted per Steve; Roslynk proof item moved out of scope.
- 2026-10-20: implement oracle (claude-opus-5-5, headless `ganda task work`): TWA0035, roles
  `[Operation]`, kind interfaces across contracts, skill reference, version bump to
  2.0.0-beta.24 with pins. Boy-scout audit fix: `ganda repo audit` failed on `nuru`
  (TimeWarp.Nuru 4.0.0-beta.2 < beta.3). `ganda repo audit --fix` moved TimeWarp.Nuru and
  TimeWarp.Nuru.DevCli to 4.0.0-beta.3, and the pin comment was updated to match. dev-cli
  compiles on beta.3, and the audit passes.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-20T06:01:45Z
- Reviewer: Grok session `01a157e6-d044-7442-b624-c74e144b8fff` (2026-10-20), effort 3, roster general, round 1
- Reviewer (round 1 retry, after the first pass exited 143): review oracle, claude-opus-5-5,
  headless `ganda task work`, effort 3, roster general, session not reported (2026-10-20).
  Opened M1 (audit gate) and requested a fix pass.

## Notes

- Follows 384 (not a child). Serial order, versioning and package contents are recorded there.

## Decisions

- **`IIdempotentCommand` does not map to PUT/DELETE by convention.** The verb stays declared
  on `[ApiRoute]`, and the kind is not derived from it. GET with `IQuery`, POST with `ICommand`,
  and PUT/DELETE with `IIdempotentCommand` is written in the skill as the usual pairing, not a
  rule. `IIdempotentCommand` promises that a retry is safe, so it goes only where the handler
  keeps that promise. `ICommand` is the safe default. The roles slice uses it for UpdateRole,
  DeleteRole, and SetRolePermissions (full replace or delete). Other slices' PUT/DELETE stay
  on `ICommand` until someone checks each handler.
- **Kind adoption rule:** a nested `Query` becomes `IQuery`, and a nested `Command` becomes
  `ICommand`. Two GET queries stay on bare `IRequest` because their kind is not clear: each
  has a side effect. `SubmitTip.Query` takes a payment and `InvokeMeteredCapability.Query`
  debits the ledger. Their Design regions say why. The SignalR chat messages (`SendMessage`,
  `ReceiveMessage`) are commands and move to `ICommand`.
- **Operation naming:** `<resource>.<action>` in lower case (`roles.list`, `roles.get`,
  `roles.create`, `roles.update`, `roles.delete`, `roles.set-permissions`), with
  `Group = "admin.roles"`.
- **TWA0035 lives in the convention analyzers.** It uses the same hosted gate as the
  generators (`HostedRouteDiscovery.TryGetHostedOperation`) and the same FQN and whitespace
  rule as the `[Operation]` readers. It reports at info severity, at the request identifier.
- `documentation/` is retired, so the skill reference
  (`skills/tw-web-api-contracts/references/neutral-operations.md`) is the only write-up.

## Results

- TWA0035 (`OperationAttributeAnalyzer`, info) is in `timewarp-architecture-convention-analyzers`.
  It has 7 Jaribu tests: a report, a clean `[Operation]`, a whitespace name, a foreign
  attribute, client-only on the request with and without `[ApiEndpoint]`, and an unrouted
  request. In the web-contracts build it reports 54 hosted requests that have no
  `[Operation]`, and none of them are in roles.
- The six roles contracts carry `[Operation(...)]`, and their Design regions say why each has
  its kind. The generated partials carry `OperationName`, `OperationDescription`, and
  `OperationGroup` (for example `roles.get` and `admin.roles`). `TimeWarp.Contracts` is a
  global using in web-contracts.
- 67 contract requests moved from `IRequest` to `IQuery`/`ICommand`/`IIdempotentCommand`, plus
  the 2 chat hub messages. Handlers are unchanged. The two side-effecting GET queries stay on
  `IRequest`, each with a stated reason.
- Proof: the roles slice and every other slice compile and pass their tests on the package as
  shipped. `TimeWarp.Contracts` did not change.
- `tw-web-api-contracts` has a new section, "Neutral operations: one contract, three bindings",
  and a new reference, `references/neutral-operations.md`. The kind interfaces now appear in
  the detection table, the verb table, the contract shell, the workflow, and the checklist.
  The examples moved to the kind interfaces. AGENTS.md's mediator rule names the kind
  interfaces and `[Operation]`.
- Version is 2.0.0-beta.24 in `source/Directory.Build.props`,
  `timewarp-templates/Directory.Build.props`, and the platform pins in
  `Directory.Packages.props`. beta.23 is already on nuget.org.
- Review: round 1, effort 3, roster general (two passes: `general.md` and a host retry,
  `general-retry.md`). Counts: bug 1 open / 0 fixed / 0 wontfix, suggestion 0, nit 0.
  Disposition: **pending (reopened)**, fix pass requested for M1. The first pass recorded
  clean. The retry re-ran the gates: `dev build`, the analyzer tests (231/231), and the roles
  runfiles pass. `ganda repo audit` fails `kanban-porcelain-gitignore` (the `.gitignore` has
  no `mission.heartbeat` line), so the audit line under "Expect" below is stale until M1 is
  fixed. The product diff has no defects. Paths: `review/review-framework.md`,
  `review/round-1/general.md`, `review/round-1/general-retry.md`,
  `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
dev build
cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release --no-build --no-restore
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release --no-build --no-restore
for f in source/container-apps/web/features/admin/roles/*/*-tests.cs; do dotnet run "$f"; done
cd tests/container-apps/web/web-contracts-tests && dotnet test -c Release --no-build --no-restore
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release --no-build --no-restore
dotnet build source/container-apps/web/projects/web-contracts/web-contracts.csproj -c Release --no-incremental --no-dependencies -p:ErrorLog=/tmp/t386.sarif,version=2.1
dev check-version
dev template-smoke
ganda repo audit
```

Expect:

- `dev build`: 0 warnings, 0 errors.
- Analyzer tests: 231 passed, including the 7 `Should_Suggest_Operation` cases. Source
  generator tests: 141 passed.
- Roles runfiles: create-role 5/5, delete-role validator 3/3, set-role-permissions 13/13,
  update-role validator 5/5.
- web-contracts-tests: 48 passed. web-server-integration-tests: 348 passed, 1 skipped.
- The SARIF has 54 `TWA0035` results at level `note`, and none name a
  `Features.Admin.Roles` type.
- `dev check-version`: `Version in source is new — safe to release` (2.0.0-beta.24).
- `dev template-smoke`: `Template smoke SUCCEEDED`.
- `ganda repo audit`: `Repository passes all audit checks.`

## Learnings

- Misread: the Goal restatement says `documentation/` is retired, so the skill is the only
  write-up of the neutral layer. The gate-lift task does not say that, and
  `documentation/developer/guides/` still holds developer guides. The checklist required the
  skill write-up, which is in `skills/tw-web-api-contracts/`, so the misread did not drop a
  checklist item.
- Round 1 retry: the rest of the restatement matches the task. The kind decision, the six
  roles operations, the three-binding write-up, and the proof scope (CLI and MCP excluded)
  all line up. The `documentation/` note above reflects an earlier repo decision to retire
  that tree, not new scope. No other misread.

