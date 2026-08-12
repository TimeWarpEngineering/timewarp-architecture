# Dual-mode TimeWarp.402 reference — template output broken since 104-011

## Parent

104 (agent-ready identity and x402 program)

## Description

Found while running 147-007's `dev template-smoke` gate: every generated app since 104-011
(commit `1c0646b7`) fails to build. `template.json` excludes `source/libraries/timewarp-402/**`
from template output (platform trees are package-mode-only, like identity), but
`web-application.csproj` carried a **bare `ProjectReference`** to it — no `UseX402Packages`
dual-mode, no CPM pin. Generated apps hit MSB9008 (missing project) + CS0234 (`TimeWarp.X402`
namespace) across the tip / metered-capability features. `web-jaribu-tests.csproj` (ships with
the web flag) had the same bare reference.

Everything else was already staged by 104-007: template-smoke packs timewarp-402 into its
smoke-local feed, and the release run packs it (`workflow-command.cs`) — only the consumer-side
wiring was missing. Last green template-smoke was 2026-08-03 on master, before 104-011 landed;
CI never caught it because these commits went to dev without a PR.

## Checklist

- [x] `UseX402Packages` auto-detect switch in root `Directory.Build.props` (mirrors
      UseIdentityPackages; Exists check on the source tree)
- [x] Dual-mode ProjectReference/PackageReference in `web-application.csproj`
- [x] Dual-mode pair in `tests/container-apps/web/web-jaribu-tests/web-jaribu-tests.csproj`
- [x] CPM pin `TimeWarp.402 = 2.0.0-beta.14` (= `<Version>`, task-124 policy; first nuget.org
      publish rides the next release — release pack list already includes the project)
- [x] template-smoke `NuGet.config` packageSourceMapping: `TimeWarp.402` → smoke-local
- [x] template-smoke pin-rewrite: `TimeWarp.402` added to `PlatformPinIncludeFragments`
      (run 2 failed NU1603 — generated pin beta.14 vs feed 2.0.0-smoke)
- [x] template-smoke harness: evict global-cache `<id>/2.0.0-smoke` entries after packing —
      run 3 failed CS1061 `ListPrincipalsAsync` because the constant smoke version let NuGet's
      global cache serve a Jul-29 TimeWarp.Identity pack; every local smoke since then silently
      tested stale platform bits (CI unaffected, cold caches)
- [x] `dev build` 0/0 + dev CLI self-install (smoke command changed)
- [x] `dev template-smoke` green — full matrix (SmokeDefault, SmokeNoPostgres, SmokeNoApi)
      after companion fixes landed under 164 (aggregator-safe runfile) and 147-007 (postgres
      template excludes, aggregator count refresh)

## Notes

- `TimeWarp.402` PackageId is deliberate (locked product name); nuget.org shows zero published
  versions as of 2026-08-05 — the pin is safe under task-124 policy because packages + template
  publish together in one release run.
- sourceName safety: "TimeWarp.402" does not contain the "TimeWarp.Architecture" sourceName
  token, so literal PackageReference/pin usage is safe (same as TimeWarp.Identity).

## Results

- Dual-mode `TimeWarp.402` wiring shipped (`4cb2f589`): `UseX402Packages` auto-detect switch in
  root Directory.Build.props; ProjectReference/PackageReference pairs in web-application.csproj
  and web-jaribu-tests.csproj; CPM pin 2.0.0-beta.14 (= <Version>, task-124 policy); smoke-local
  feed packageSourceMapping. Release pack list already carried the project, so the first
  nuget.org publish of TimeWarp.402 rides the next release automatically.
- Smoke-harness reliability fixes shipped alongside: TimeWarp.402 added to the CPM pin-rewrite
  fragments, and `PurgeStaleSmokeCacheEntries` (`7b0cf905`) evicts `<id>/2.0.0-smoke` from the
  global NuGet cache after packing — before this, every local template-smoke since Jul 29 was
  silently testing stale platform packs (CI unaffected, cold caches).
- Verified: `dev build` 0/0; full `dev template-smoke` matrix SUCCEEDED (first green since
  2026-08-03, and first ever including the x402 features in package mode).

### How to validate

**Automated**
```bash
./bin/dev template-smoke
# expect: Template smoke SUCCEEDED (SmokeDefault, SmokeNoPostgres, SmokeNoApi all OK)
```
Confirms: generated apps restore TimeWarp.402 from the packed feed (dual-mode), pins rewritten,
no stale cache reuse (look for "evicted stale cache:" lines in output).

**Spot check (package mode wiring)**
```bash
grep -n "UseX402Packages" Directory.Build.props source/container-apps/web/projects/web-application/web-application.csproj
# expect: auto-detect switch + conditional reference pair
```

Depends on: next release publishes TimeWarp.402 to nuget.org (pack list already includes it —
verify on the release run per merging-is-not-releasing policy).

## Session

- 2026-08-05 claude: found during 147-007 close-out gates; wired dual-mode per identity pattern;
  template-smoke rerun pending.
- 2026-08-05/06 claude: pin-rewrite + cache-eviction harness fixes added after runs 2-3; full
  smoke matrix green on run 8 (companion fixes: 164 runfile, 147-007 excludes/counts); done.
