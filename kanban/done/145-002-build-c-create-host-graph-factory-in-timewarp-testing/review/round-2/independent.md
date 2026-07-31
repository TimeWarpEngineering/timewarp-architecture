# Round 2 — independent verification (145-001 + 145-002)
**Date:** 2026-07-31
**Reviewer:** independent agent (Claude orchestrator session), isolated worktree detached at cccbd49a
**Scope:** origin/dev..dev (6ed38388 docs + cccbd49a factory)

## Verification results

| Claim | Observed | Verdict |
|---|---|---|
| dev build 0/0 | 0/0 (23.9s) | CONFIRMED |
| Full dev test green | 17/19 pass; **api-jaribu-tests total:6 failed:4** → `Tests failed!` | **REFUTED** |
| Weather exemplar standalone 2/2 | 2/2 | CONFIRMED |
| host-graph smoke standalone 2/2 | **0/2** — OptionsValidationException SampleOptions:SampleOption | **REFUTED** |
| api-jaribu-tests 4/4 | 2 succeeded / 4 failed (same exception) | **REFUTED** |
| web-jaribu-tests 5/5 | 5/5 | CONFIRMED |
| template-smoke | **fails at SmokeDefault tier 2** (same exception); SmokeNoApi never reached; round-1 never ran smoke | **REFUTED** |
| Boot order Api→Web→Yarp per findings | factory matches evidence | CONFIRMED |
| repo audit (145-001) | 23/23 | CONFIRMED |

**Root cause (one defect, three reproductions):** WebTestServerApplication's new
`ContentRootPath = dir(Web.Server IAssemblyMarker Assembly.Location)` only resolves
Web.Server's appsettings when Web.Server is a DIRECT reference of the running process
(web-server-integration-tests — why 97 Fixie tests still pass). Transitive consumers
(standalone runfile, api-jaribu-tests aggregator, generated app under smoke) throw
OptionsValidationException at Host.StartAsync before Kestrel binds.

## Round-1 audit

Round-1 was an effort-1 SELF-review by the implementer; its verification table asserts the
two refuted results verbatim, and template-smoke was never run despite harness count changes.
Insufficient — two tasks were closed and the epic queued on a primitive whose primary use
case fails 100% of the time outside one legacy suite.

## Summary

145-001 docs: sound, all four requirements met. 145-002: C-create core design CORRECT
(no statics, fresh graphs, Api→Web→Yarp boot, reverse dispose, rollback-on-failure, genuine
WebApplicationHost fault-surfacing improvement) but the Web+Api path is broken for every new
consumer; task should not have been marked done.

## Issues

### R2-1 — Severity: bug — Status: open
- web-test-server-application.cs ContentRootPath transitive-dependency failure (root cause
  above). Fix must make Web.Server's appsettings resolve for ANY consumer (embed/copy-via-
  MSBuild/absolute source path — implementer determines mechanics + why round-1's env passed).

### R2-2 — Severity: bug — Status: open
- host-graph-factory-tests.cs lives under api/features/** guarded only by (!api); hard-refs
  Web types with no #if (web) guard and no (!web) exclude → `--api true --web false`
  generation cannot compile (no SmokeNoWeb matrix exists to catch it).

### R2-3 — Severity: suggestion — Status: open
- Placement: file tests timewarp-testing infra, not an api product slice; its own Design
  region admits placement was chosen for aggregator-globbing convenience. Violates the
  AGENTS.md deletion litmus; ships to every api-flagged generated app. Structural fix also
  resolves R2-2.

### R2-4 — Severity: suggestion — Status: open
- Mock proof is registration-only (ShouldBeOfType), not behavioral (no authenticated request
  consuming the mock token).

### R2-5 — Severity: nit — Status: open
- Stale "(web 5, api 2)" Design-region comments in template-smoke-command.cs:29 +
  template-smoke-harness.cs:21 vs actual api expected 4.
