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

## Session

- Created: Claude (2026-08-05, during live incident diagnosis)
