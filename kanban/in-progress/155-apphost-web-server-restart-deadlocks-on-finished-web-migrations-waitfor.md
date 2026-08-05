# AppHost web-server restart deadlocks on Finished web-migrations WaitFor

## Description

Observed twice on 2026-08-05 in a live `dev run`: executing the dashboard **restart** (or
**rebuild**) command on the `web-server` resource stops the process and then hangs forever in
`Waiting for resource 'web-migrations' to enter the 'Running' state.` — because the
`web-migrations` EFMigration resource is in state **Finished** after its startup run and never
re-enters Running by itself. The web-server's `WaitFor(web-migrations)` (AppHost) is therefore
only satisfiable on first boot; every subsequent restart of web-server deadlocks, leaving the
site down until the operator notices.

Workaround that unblocks it: manually execute `ef-database-update` on `web-migrations` (its
tool run transitions the resource through Running → Finished, satisfying the wait). Verified
working both times.

## Requirements

- `restart` and `rebuild` on web-server complete without manual intervention while the AppHost
  keeps running.
- Migration-before-serve ordering on FIRST start is preserved (web-server must not race the
  initial schema migration).
- Investigate the idiomatic Aspire 13.4 fix: `WaitForCompletion` vs `WaitFor` semantics for
  Tool/EFMigration resources on restart, or restart-triggered re-run of the migration
  resource, or dropping the WaitFor edge from the restart path if the SDK supports it.

## Checklist

- [ ] Reproduce: `dev run`, dashboard → web-server → Restart; observe hang in console logs
- [ ] Determine correct wait primitive (`WaitForCompletion(web-migrations)` is the likely fix —
      Finished/exit-0 should satisfy it) or restart semantics in AppHost program
- [ ] Implement in `aspire-app-host` program; reconcile Design regions
- [ ] Validate: restart web-server from dashboard completes; first-boot ordering still holds
      (fresh volume: migrations run before web-server serves)
- [ ] Results with How to validate

## Notes

- Secondary fallout documented in this session: a hung web-server rebuild left half-written
  static asset outputs, which later caused the stale staticwebassets manifest / gzip variant
  mismatch (empty `dotnet.js` responses). Fixing the deadlock removes that trigger path.
- Related repo footgun (no task yet, behavioral note): running `dev build` while `dev run` is
  live rewrites `bin/Debug` static assets under the running server; the in-memory manifest then
  mismatches on-disk compressed variants and gzip-accepting clients get empty 200s. Remedy:
  rebuild + restart web-server (blocked by this very deadlock until fixed).

## Implementation Plan (Phase 2, 2026-08-05)

Root cause and primitive semantics verified by decompiling the installed packages
(Aspire.Hosting 13.4.6, Aspire.Hosting.EntityFrameworkCore 13.4.6-preview.1.26319.6):

- `web-migrations` (EFMigrationResource) state machine: NotStarted → Waiting → Running →
  Finished/FailedToStart (or back to NotStarted on cancel). It never publishes an ExitCode and —
  despite its README/XML docs — registers NO health check in this package version.
- `WaitFor` creates WaitUntilHealthy; run mode with dashboard uses
  `WaitBehavior.WaitOnResourceUnavailable`, whose only continuable state is `Running`. On
  web-server restart the orchestrator re-runs waits, WatchAsync replays the last snapshot
  (`Finished`), and `Running` never arrives → infinite hang (the observed message).
- Bonus defect: `WaitFor` was satisfied when the migration entered Running, so even first boot
  never actually guaranteed migration-COMPLETION-before-serve.

**Fix: `webServer.WaitForCompletion(webMigrations)`** —
`WaitUntilCompletionAsync` accepts terminal states (`Finished` ∈ TerminalStates) and WatchAsync
replays the last snapshot on every wait, so restart resolves immediately; first-boot ordering
becomes strictly stronger (waits for Finished, not Running); null ExitCode makes the
exit-code guard inert; migration failure surfaces as web-server FailedToStart instead of a hang.
This also re-converges code with ADR 0009 line 74 which always specified WaitForCompletion.

Rejected: `WaitFor(…, StopOnResourceUnavailable)` (throws "entered Finished prematurely" on
restart), `WaitForStart` (same machinery), custom health check (still requires Running first),
eventing re-run hook (races RunDatabaseUpdateOnStart; kept only as fallback), dropping the edge
(violates first-boot ordering).

Edits:
1. `source/container-apps/aspire/projects/aspire-app-host/program.cs` (inside `#if postgres`,
   directives untouched): line ~145 WaitFor → WaitForCompletion; rewrite the adjacent comment
   and the file's Design region (its "WaitFor uses the health check registered by
   RunDatabaseUpdateOnStart" claim is factually wrong for the shipped package; the 147-007
   "same-project WaitForCompletion breaks DCP service-producer annotations under
   Aspire.Hosting.Testing" claim is historical/thin — re-verify via suites, date-stamp findings).
2. `documentation/developer/how-to-guides/how-to-add-your-aggregate.md` line ~207: "WaitFor
   (migration resource healthy)" → "WaitForCompletion (migration resource Finished)" (line 232
   already says WaitForCompletion; doc was internally inconsistent).

Validation gates:
- `dev build` 0/0.
- Fresh-volume first boot: web-server waits (log: "Waiting for resource 'web-migrations' to
  complete.") until Finished, then serves; schema exists.
- THE BUG: dashboard restart of web-server completes unaided; repeat with rebuild and
  stop/start. Mid-run edge: restart web-server while ef-database-update is Running → waits for
  Finished then starts.
- 147-007 regression gate: run `aspire-tests`, `web-spa-integration-tests`,
  `api-server-integration-tests`; watch for DCP service-producer annotation errors and 2-minute
  fixture timeout pressure. If the DCP failure reproduces: revert to WaitFor + eventing hook
  fallback and file upstream dotnet/aspire.

## Plan Amendment (Phase 4 design stop, 2026-08-05)

The WaitForCompletion premise FAILED validation: with
`webServer.WaitForCompletion(webMigrations)`, aspire-tests fails 6/7 under
Aspire.Hosting.Testing with the historical 147-007 error, reproduced verbatim:
`Could not create Endpoint object(s): Error = information about the port to expose the
service is missing; service-producer annotation is invalid` (on web-server, immediately after
the completion wait resolves). The plan's "147-007 concern is historical" claim was wrong —
the regression is real on Aspire 13.4.6 with the same-project migration resource.

**Maintainer decision (Steve): hybrid on-demand migrations.** Production never uses the
AppHost auto-run path (pipeline artifacts apply migrations), so the wait edge existed only for
dev first-boot. Resolution:
- Keep `web-migrations` + `RunDatabaseUpdateOnStart` (first-boot OOBE self-heals the schema).
- REMOVE the wait edge entirely (no WaitFor, no WaitForCompletion) — restart deadlock is
  impossible by construction; the DCP testing bug becomes irrelevant.
- Accepted tradeoff: on a truly fresh volume, web-server may serve for a few seconds before
  the initial migration completes (DB-backed pages error briefly).
- On-demand surface: `ef-database-update` dashboard command on web-migrations.
- ADR 0009 amended; how-to-add-your-aggregate updated to match.
- Follow-up scope (not this task): `dev db-update` CLI wrapper executing the resource command
  against the running AppHost.
- Considered and rejected: WaitFor + restart-triggered re-run eventing hook (extra
  orchestration code for semantics prod doesn't have); env-conditional wait (test/run topology
  divergence in a template repo).

## Plan Amendment 2 (Phase 4 continued, 2026-08-05)

Hybrid implementation (no wait edge) built and passed `dev build` 0/0, but the aspire-tests
regression gate was 6/7: `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
consistently (3/3 runs) got `401 Unauthorized` instead of the expected `403 Forbidden`. Traced
to `PrincipalRoleClaimsTransformation` → `EffectiveRolesResolver` → `EfPrincipalRoleStore`
hitting Postgres before `RunDatabaseUpdateOnStart` finished — exactly the accepted first-boot
race, but deterministic here because `aspire-tests` always boots an ephemeral Postgres
(`--Postgres:UseDataVolume=false`), so migrations always have real work to do, unlike a real
dev volume which is already-current after its first boot.

**Maintainer decision (Steve), after options discussion:** confirmed this is DX-only (production
never runs the AppHost auto-run path at all; a real dev volume only pays the race once, and it
self-heals in seconds). Rejected building AppHost-level readiness gating (a stronger health
check + `.WithHttpHealthCheck()` wiring + loosening the `/health` endpoint's
`IsDevelopment()`-only guard) as disproportionate to a rare, self-healing, once-per-volume
window, and as reopening a real security-posture question (unauthenticated `/health` exposure)
for a benefit that doesn't justify it.

**Fix implemented:** test-side only, in `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`
`SetupOnce` — wait for the `web-migrations` resource to reach a terminal state
(`Aspire.Hosting.ApplicationModel.KnownResourceStates.TerminalStates`) via
`ResourceNotificationService.WaitForResourceAsync` (a notification-service **poll** from test
code against the already-running graph, not a builder-graph `WaitFor`/`WaitForCompletion`
annotation) before firing DB-backed requests. Reproduces neither the restart-deadlock nor the
DCP service-producer bug, since it never touches the AppHost's resource-graph wait wiring.
Added `global using Aspire.Hosting.ApplicationModel;` to the test project for
`KnownResourceStates`.

**Follow-up spun out (not blocking this task):** task 158 — the 401-vs-403 mislabeling itself is
a real, independent bug (any transient DB failure during role resolution, not just this race,
would hit the same code path in production) and needs its own fix + deterministic test, agreed
as separate scope.

## Plan Amendment 3 (baseline verification, 2026-08-05)

**Correction to Amendment 2's diagnosis.** Before treating the `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
failure as caused by this task, verified against the true baseline: `git stash push -u` (all of
today's task-155 changes, including the test-side migration wait), `dev build` (0/0), ran
`aspire-tests` 3× on the **unmodified original code** (`WaitFor(webMigrations)`, no test
changes). **Same failure, 3/3 runs, identical symptom** (401 instead of 403). `git stash pop`
restored the working tree to its task-155 state (verified clean restore, `dev build` 0/0 again).

Conclusion: this test failure **predates task 155 and is not caused by its wait-edge change**.
The original `WaitFor(webMigrations)` already guarantees migrations reach at least `Running`
before web-server starts, and by the time any test method fires the migration is long finished
either way — so this was never actually a migration-timing race, disproving Amendment 2's
causal claim. Task 158 has been corrected to remove the now-disproven migration-race theory and
reopen root cause as unknown (pre-existing bug, not task-155 scope). The test-side
`WaitForResourceAsync` wait added to `ingress-smoke-tests.cs` in Amendment 2 is kept — it's
harmless and correctly guards the *actual* (if narrow) migration-race window this task's design
introduces for other DB-backed assertions in that suite — but it does not fix, and was never
going to fix, this specific pre-existing failure.

**This task's own gates are unaffected:** `dev build` 0/0 with task 155's changes in place;
`aspire-tests` shows the same single pre-existing failure with or without task 155's changes
(no regression introduced); `web-spa-integration-tests` and `api-server-integration-tests`
green. Task 155 closes on that basis — the pre-existing `aspire-tests` failure is tracked
separately by task 158, not by this task.

## Session

- Created: Claude (2026-08-05, during live incident diagnosis)
- 2026-08-05 claude (orchestrator): Phase 2 plan complete (Plan agent, decompilation-backed);
  proceeding to implement.
- 2026-08-05 claude (orchestrator): implementer reproduced 147-007 DCP failure and stopped
  clean per plan; design stop escalated to maintainer; hybrid approved; implement resumed.
- 2026-08-05 claude (orchestrator): hybrid implemented; aspire-tests showed a 401-vs-403
  mismatch initially (mis-)attributed to a migration-race; baseline verification (stash/rerun on
  unmodified code) disproved that — failure is pre-existing, unrelated to this task; task 158
  corrected accordingly; task 155 gates re-confirmed clean on that basis.
- 2026-08-05 claude (orchestrator): hybrid implemented; aspire-tests found a second, real
  first-boot race (role-resolution DB query vs. migrations) via a 401-vs-403 mismatch; discussed
  options with maintainer (Steve); test-side terminal-state wait implemented; task 158 spun out
  for the 401-mislabeling bug itself.
