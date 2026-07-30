# Round 1 — general
**Date:** 2026-07-29
**Scope reviewed:** branch Claude/2026-07-29/adopt-co-located-jaribu-tests vs dev

## Verification results

| Claim | Observed | Result |
|---|---|---|
| Clean build 0/0 | dotnet clean + build slnx: 0 Warnings / 0 Errors (34.9s) | CONFIRMED |
| analyzers-tests 102/0 | 102 passed | CONFIRMED |
| sourcegenerator-tests 59/0 | 59 passed | CONFIRMED |
| create-role-tests.cs 5/5 standalone | 5/5, exit 0 | CONFIRMED |
| get-weather-forecasts 2/2, :7255 | real host, 200+400 observed, 2/2, port free after | CONFIRMED |
| NEGATIVE `foo-handler-tests.cs` → TWA0015 | Fires when the file itself is built (`dotnet build <file>.cs`: error TWA0015 'handler'→'-application'); does NOT fire via solution `dev build` (unrouted layer compiles into no project) | CONFIRMED (mechanism) / enforcement-surface caveat → Issue 1 |
| NEGATIVE `foo-bogus.cs` → guard | Solution build hard error: "match NO registered layer suffix … layer one of: -contracts, -application, -domain, -infrastructure, -server, -tests." | CONFIRMED — no hole in the guard |
| No Compile glob for -tests.cs; 3 families consistent | regex alternation includes tests; `FeatureFilenameGrammarLayer Include="tests"` with NO Project= ; zero -tests Compile ItemGroups | CONFIRMED |
| cnd:noEmit matches web-spa/program.cs precedent | identical form | CONFIRMED |
| Tier-2 harness fails on real regression | injected assertion failure: exit 1, Total 5 / Passed 4 — both Success and passed!=total checks catch it (reverted) | CONFIRMED |
| No new audit violations from this diff | kebab-path-names failures all pre-existing; runfile-* checks PASS | CONFIRMED |

## Summary

All claimed numbers verified exactly, including a clean full rebuild. Negative probes behave
correctly: guard still catches orphaned files after registering `tests`; TWA0015 fires on
mis-paired `-handler-tests.cs` — but only when that runfile is individually compiled, never
via the mandatory `dev build` gate (unrouted files are in no project's Compile glob). Generator,
analyzer-test, and template-smoke changes strengthen rather than weaken assertions; the tier-2
failure path was proven end-to-end by injecting and reverting a real test failure. Two
non-blocking gaps: ported runfiles not `chmod +x`; docs point to a `tw-jaribu` skill that this
task deliberately did not edit (dead-end pointer).

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-feature-placement/SKILL.md:225-238, AGENTS.md:143-149
- Description: Docs say `-handler-tests.cs` "still trips TWA0015" — true but incomplete:
  TWA0015/16 for `-tests.cs` files fires only when the file itself is compiled
  (standalone `dotnet build`/`run`), never via the mandatory `dev build` solution gate; only
  the two exemplar files are exercised by template-smoke. A future mis-paired `-tests.cs`
  would not be caught by mandatory PR gates.
- Suggestion: one clarifying sentence in AGENTS.md/SKILL.md that enforcement is per-file
  opt-in until aggregators (task 136) restore build-time coverage.
- Status: open

### Issue 2 — Severity: nit
- File: both ported `-tests.cs` files
- Description: shebang'd runfiles committed 100644 (not executable) — `./file.cs` → Permission
  denied; repo's other runfiles are 100755.
- Suggestion: `chmod +x` both before merge.
- Status: open

### Issue 3 — Severity: suggestion
- File: AGENTS.md:57, documentation/developer/standards/file-naming.md:33-36, skills/tw-feature-placement/SKILL.md:376-377
- Description: All three docs point to the `tw-jaribu` skill for the preamble convention, but
  that skill is cross-repo and was deliberately not edited; no `skills/tw-jaribu/` exists in
  this repo. The canonical preamble lives only in exemplar-file comments and kanban plan.md.
- Suggestion: inline the canonical preamble block in-repo (AGENTS.md or SKILL.md) or point at
  the two exemplar files as canonical until the cross-repo skill is updated.
- Status: open
