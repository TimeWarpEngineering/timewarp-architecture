# Round 2 — independent post-hoc review (orchestrator)

**Date:** 2026-07-24
**Scope:** Grok's full 113 implementation (13 commits on dev: GoldenDbContext extraction,
Profile teaching aggregate, ADR-0009, HowToAddYourAggregate, children 113-003/004/005).
Round 1 was the build agent's self-review; this round is the independent verification pass
with empirical re-runs.

## Verified green (re-run, not taken from Results)

- `dev build` — 0 warnings / 0 errors
- foundation-infrastructure-tests — 9/9 (InMemory golden-hook tests: DomainInvariantsGuard,
  Version bump, child→root resolution incl. deleted-child and added-child)
- web-infrastructure-tests — 5/5 (connection-free model mapping + live Postgres round-trips;
  Docker present, so the live tests actually ran; fail-closed CI-skip polarity checked)
- web-server-integration-tests — 97 passed / 1 skipped; aspire-tests — 7/7
- ADR-0009, HowToAddYourAggregate, disposition.md all present and consistent with the code

## Findings

### R2-1 (major, fixed): template-smoke gate never run — and it failed

Grok's verification listed only `dev build` + two unit suites. `dev template-smoke` failed
on BOTH matrices, for two distinct causes:

1. **SmokeDefault** — `postgres-db-context.cs` carried a literal
   `using` of the TypedId generator's EF namespace. dotnet-new sourceName-rewrites that
   literal to `<AppName>.TypedIds.Ef`, but the generator's emitted namespace is a baked-in
   constant (`EfNamespace` in typed-id-source-generator.cs) that never rewrites → every
   generated app failed CS0234. **Fix (task-115 pattern):** composed
   `TwArchitectureTypedIdsEfNamespace` property in `msbuild/timewarp-platform-packages.props`
   + unconditional MSBuild `<Using>` in `web-infrastructure.csproj`; literal using removed.
   Unconditional because, unlike the Attributes namespace, this one is identical in source
   and package mode.
2. **SmokeNoPostgres** — the new `web-infrastructure-tests` project (pure Profile/Postgres
   content) shipped unconditionally: slnx gated it on `web` only, and template.json's
   `(!postgres)` exclude never listed it → CS0246 `PostgresDbContext` with the flag off.
   **Fix:** nested `<!--#if (postgres) -->` around the slnx entry +
   `tests/container-apps/web/web-infrastructure-tests/**` in the `(!postgres)` exclude.

After both fixes: `dev build` 0/0, `dev template-smoke` SUCCEEDED (both matrices),
web-infrastructure-tests 5/5.

Red herring worth recording: the first smoke runs also showed
`TimeWarp.Foundation.Persistence` / `GoldenDbContext` missing — that was the known
stale-2.0.0-smoke NuGet-cache failure mode (same as in 107's smoke), not a packaging bug.
The freshly packed `TimeWarp.Foundation.Infrastructure` nupkg was verified to contain
GoldenDbContext. Cache cleared; errors gone.

Also found en route: the AOT-installed `dev` binary predated the `template-smoke` route
(silently "Unknown command") — refreshed via `dev self-install`.

### R2-2 (process, fixed): M2 wontfix follow-on was never filed

Round 1 dispositioned M2 (auto-`IsConcurrencyToken` enforcement) as wontfix-with-follow-on,
but no task existed. Filed **task 121** with the enforcement options (runtime model check in
GoldenDbContext vs Roslyn analyzer vs both) laid out for decision.

## Verdict

Implementation substance is sound — the golden hook, child→root gap closure, teaching
aggregate, and docs all check out empirically. The gaps were both in the template seam
(exactly the class of regression `dev template-smoke` exists to catch) plus one process
miss. All fixed and verified this round. 113 stays done.

## Lesson

Any change touching template content (`source/**`, `tests/**`, slnx, template.json) is not
done until `dev template-smoke` runs green — build agents must include it in verification
whenever their diff adds files or namespaces under template-managed paths.
