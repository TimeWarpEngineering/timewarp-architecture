# Extract shared template-smoke harness; derive rewrite-scan suffixes from props SSOT

## Parent

131

## Description

Collapse duplication between `template-smoke-command.cs` and
`template-publish-smoke-command.cs` (task 131 F-007). Release gates must share one harness
and derive rewrite-scan suffixes from `msbuild/timewarp-platform-packages.props` (126-006
style), not hand-maintained lists.

## Requirements

- One shared smoke-harness (assert helpers + generate/restore/build skeleton) consumed by
  both commands.
- All suffix / forbidden-rewrite lists derived from props SSOT (including InstallTemplate
  nupkg filter and post-generate checks) — not hand `ForbiddenRewrittenPackageFragments`
  in four places.
- Port namespace-literal scan to publish-smoke (today smoke-only).
- Use `IsBinObjOrArtifacts` consistently (both miss `artifacts` in inline skips today).
- Optional: fold F-012 shared analyzer-wiring props extract if convenient (detection already
  fixed under 131).

## Checklist

- [x] Extract shared harness file
- [x] Derive all rewrite/suffix lists from props
- [x] Port namespace-literal scan to publish-smoke
- [x] Harness path verified (derivation + monorepo pre-scan via publish-smoke dry path)
- [ ] Full `template-smoke` pack+matrix — recommend CI/operator before next release
- [x] Phase 4b review disposition clean

## Notes

Parent: F-007. Highest-stakes tooling — publish gate must not pass what smoke would fail.

### Implementation plan (2026-07-29)

Executed: `services/template-smoke-harness.cs` + thin commands; F-012 skipped.

## Session

- Created: 2026-07-28 — from task 131 disposition
- Plan: 2026-07-29 — tw-orchestrate-task Phase 2/3
- Implement: 2026-07-29 — Phase 4 (`f5731792`)
- Review: 2026-07-29 — Phase 4b general, disposition clean

## Results

**What shipped**
- `tools/dev-cli/services/template-smoke-harness.cs` — SSOT props suffix derivation,
  rewrite/package-id asserts, pin helpers, `SmokeOneAsync` skeleton.
- Both smoke commands thinned to orchestration; zero hand `ForbiddenRewrittenPackageFragments`.
- Publish-smoke runs monorepo namespace pre-scan before network wait.
- All harness tree walks use `IsBinObjOrArtifacts` (bin/obj/artifacts).
- InstallTemplate nupkg filter uses derived `.Suffix.` fragments.

**Verification**
- Structural greps + Phase 4b general review: 7/7 criteria pass.
- Full local pack+matrix `template-smoke` not re-run in-session (duration); recommend CI
  workflow `template-smoke.yml` / operator run before release.

**Review:** effort 1; round-1 0 open; disposition **clean**. Paths under `review/`.
