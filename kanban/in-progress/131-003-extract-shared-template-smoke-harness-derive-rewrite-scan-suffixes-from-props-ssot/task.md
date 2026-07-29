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

- [ ] Extract shared harness file
- [ ] Derive all rewrite/suffix lists from props
- [ ] Port namespace-literal scan to publish-smoke
- [ ] `dev template-smoke` (or equivalent) green
- [ ] `dev` publish-smoke path green when packages available

## Notes

Parent: F-007. Highest-stakes tooling — publish gate must not pass what smoke would fail.

## Session

- Created: 2026-07-28 — from task 131 disposition
