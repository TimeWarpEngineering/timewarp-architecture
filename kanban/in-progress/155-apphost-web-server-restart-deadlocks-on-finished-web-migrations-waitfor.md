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

## Session

- Created: Claude (2026-08-05, during live incident diagnosis)
- 2026-08-05 claude (orchestrator): Phase 2 plan complete (Plan agent, decompilation-backed);
  proceeding to implement.
