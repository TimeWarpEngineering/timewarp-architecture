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
- [x] `dev build` 0/0 + dev CLI self-install (smoke command changed)
- [ ] `dev template-smoke` green (rerun in progress)

## Notes

- `TimeWarp.402` PackageId is deliberate (locked product name); nuget.org shows zero published
  versions as of 2026-08-05 — the pin is safe under task-124 policy because packages + template
  publish together in one release run.
- sourceName safety: "TimeWarp.402" does not contain the "TimeWarp.Architecture" sourceName
  token, so literal PackageReference/pin usage is safe (same as TimeWarp.Identity).

## Session

- 2026-08-05 claude: found during 147-007 close-out gates; wired dual-mode per identity pattern;
  template-smoke rerun pending.
