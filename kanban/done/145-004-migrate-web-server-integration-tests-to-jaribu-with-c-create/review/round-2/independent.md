# Round 2 — independent verification (145-004)
**Date:** 2026-08-02
**Reviewer:** independent agent (orchestrator session), clean worktree at 54b3ff1a

## Verdict: REFUTED — two blocking defects reached done

| Claim | Verdict |
|---|---|
| dev build 0/0 | CONFIRMED |
| Suite 95 succeeded (+skip) | CONFIRMED |
| Hello standalone 2/2; web-jaribu 7/7; harness web count 5→7 | CONFIRMED |
| Full dev test green | CONFIRMED |
| Method-level test-inventory parity (97→95+2) | CONFIRMED — zero silent losses (only Fixie Setup() hooks correctly inlined away) |
| Triage (only hello co-located) | CONFIRMED reasoned, policy-consistent |
| Wall-clock faster than Fixie | CONFIRMED (sanity) |
| template-smoke (omitted from round-1 gates — 5th consecutive omission) | **REFUTED**: SmokeNoApi build fails — 21× CS0117 CreateWebWithApiAsync (method is #if(web && api); suite calls it unconditionally; suite not excluded under (!api)). NEW regression vs Fixie (DI wiring never named api types; compiled web-only). |
| ganda repo audit PASS | **REFUTED**: fails cpm-consistency 22/23 — the 4 "restored" xUnit-family pins (54b3ff1a) are orphaned; 145-003 removed them CORRECTLY; removing again returns 23/23 |

## Issues

### R2-1 — bug (blocking) — Status: open
SmokeNoApi CS0117 ×21: unconditional CreateWebWithApiAsync calls vs #if(web && api) guard;
template.json (!api) excludes don't cover web-server-integration-tests. Fix options (implementer
verifies which is honest): (a) exclude suite under (!api) — declares suite api-required, loses
web-only compile coverage pre-migration had; (b) add CreateWebAsync (#if(web)) to the factory +
cnd-escaped template conditionals at call sites — preserves web-only degradation parity.
Check whether hello-tests.cs shares the same dependency (it feeds the web aggregator which
SHIPS under --api false).

### R2-2 — bug — Status: open
Remove the 4 re-added CPM pins (coverlet.collector, Microsoft.NET.Test.Sdk, xunit,
xunit.runner.visualstudio) — orphaned, audit-breaking; 145-003's removal was correct.

### R2-3 — suggestion — Status: open
Add hello-tests.cs to CoLocatedTestFiles ("web" family) so tiers 1/2 cover it, after R2-1
resolves its flag posture.

## Round-1 audit

Self-review claimed audit PASS and gates green; both refuted by reproduction. Fifth
consecutive Grok self-review omitting template-smoke. Independent round-2 remains mandatory.
