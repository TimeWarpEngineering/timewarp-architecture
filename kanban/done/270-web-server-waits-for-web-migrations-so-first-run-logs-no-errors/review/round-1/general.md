# Round 1 — general
**Date:** 2026-10-02
**Scope reviewed:** same as framework (product code, tests, template.json exclude, skill text, Design regions)

## Summary

The change keeps the AppHost graph unchanged (WaitForCompletion re-tested on 13.6 and rejected with the
recorded DCP substitution error) and makes `SiteSettingsSeedHostedService` ask a new optional
`ISiteSettingsTableProbe` (`to_regclass` catalog lookup, names from the EF model, passed as a SQL
parameter — no injection surface) before its EF read, so a first run against an empty database never
executes the failing query that EF logs at Error. Risk is low. Verified against the code:

- Retry loop: the probe is skipped on the last attempt, so an exhausted budget still fails the host
  with the real 42P01 (the `when (attempt < MaxAttempts …)` filter rethrows); the 42P01 catch remains
  for the probe→query race.
- Probe failures other than "missing" (e.g. connection) propagate exactly as the seeder's own query
  did before — no behavior change there; `PostgresRetryPolicy` still applies.
- In-memory builds: interface lives in application (always compiled); the EF probe file is added to
  the `!postgres` exclude in template.json and only `PostgresDbModule` registers it, so in-memory
  resolves null and does a single pass (`GetService`, not `GetRequiredService`).
- Tests: aspire-tests fact captures web-server console from resource creation (backlog replay) and
  strips ANSI before matching `fail:`/`crit:`/`42P01`; it was shown red before the fix. The
  web-infrastructure probe test pins a control case so the log capture cannot pass vacuously.
  Per-test `probe_<guid>` databases are not dropped, matching the existing sibling store tests on the
  same disposable server.
- Design regions (AppHost, postgres module, seed service, seed-on-read decorator, ingress smoke) and
  the `tw-aggregate-pattern` skill line are consistent with the new behavior; no stale "DB-backed pages
  error for a few seconds" claim remains.

## Issues

<!-- None found. -->
