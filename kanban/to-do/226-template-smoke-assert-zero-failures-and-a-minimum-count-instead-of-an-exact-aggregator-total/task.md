# Template smoke: assert zero failures and a minimum count instead of an exact aggregator total

## Description

`tools/dev-cli/services/template-smoke-harness.cs` `JaribuFamilyAggregators` pins an exact
`ExpectedSucceeded` per generated Jaribu aggregator (`web` 148 → 170 → 180, `api` 9, `common` 3)
and fails the smoke when the MTP summary total differs. Every task that adds a co-located test
turns the template smoke red until someone bumps the literal: 219-006 (148 → 170) and 225
(170 → 180) both hit it, each costing a review-fix round and a CI rerun. The check's purpose is
"the generated app's aggregator actually ran the co-located suite and nothing failed", not "the
suite has exactly N tests".

## Requirements

- Replace the exact-equality assertion at ~L857 with: `failed == 0` (parse the MTP `failed:`
  line; fail if absent) **and** `succeeded >= MinimumSucceeded` **and** `total == succeeded + skipped`
  (parse `skipped:`; treat missing as 0). Keep `exit code == 0` as a precondition. Fail the smoke
  if the summary block cannot be parsed at all (the aggregator may have silently discovered zero
  tests).
- Rename the tuple field `ExpectedSucceeded` → `MinimumSucceeded` and set the current values as
  floors (web 180, api 9, common 3). Update the comment to say "floor; raise deliberately, never
  needs bumping when tests are added". Also emit an Info line with the actual total so the log
  still shows growth.
- Guard against the opposite drift: if `succeeded` is more than 2× the floor, print a Warning
  (not a failure) suggesting the floor be raised, so the floor stays meaningful.
- Add regexes for `failed:` and `skipped:` next to the existing `Total:`/`Passed:` generated
  regexes (note MTP prints lowercase `total:`/`succeeded:`/`failed:`/`skipped:` — check what the
  current regexes actually match against the captured output and align).
- Tests: `tests/tools/dev-cli-tests/` Jaribu tests for the decision helper (pure function over
  parsed numbers): pass at floor, pass above floor, fail on any failed, fail on unparsable
  summary, warn above 2× floor, fail when total ≠ succeeded + skipped.
- Docs: update `documentation/developer/guides/releasing.md` or wherever template-smoke is
  described if it mentions the exact-count rule.

## Depends on

- 225

## Checklist

- [x] Floor + zero-failure assertion; parse failed/skipped; unparsable summary fails
- [x] `MinimumSucceeded` rename and comment; actual total logged; 2× floor warning
- [x] Jaribu tests for the decision helper
- [x] `dev template-smoke` passes locally (SmokeDefault / SmokeNoPostgres / SmokeNoApi)
- [x] `dev build` 0/0; `ganda repo audit` clean
- [x] Results and How to validate

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: grok (2026-09-16)

## Notes

- Evidence: PR #365 (219-006) run 34981521704 and PR #371 (225) run 35055916884, both
  "aggregator did not report N/N (exit 0; parsed total=M, succeeded=M)" with M > N and zero
  failures.
- Files: `tools/dev-cli/services/template-smoke-harness.cs` (~L566-606 and ~L840-870),
  `tests/tools/dev-cli-tests/`.

## Results

Template-smoke tier 3 no longer pins an exact MTP succeeded count. The generated
Jaribu aggregators must exit 0, report `failed == 0` (`failed:` required),
`succeeded >= MinimumSucceeded` (floors: web 180, api 9, common 3), and
`total == succeeded + skipped` (missing `skipped:` is 0). An unparsable summary
fails. Succeeded above 2× the floor is a warning only. The smoke log prints the
actual MTP totals so growth is still visible without bumping literals.

**Files**

- `tools/dev-cli/services/jaribu-aggregator-summary-gate.cs` — parse + decide helper
- `tools/dev-cli/services/template-smoke-harness.cs` — `MinimumSucceeded` floor gate
- `tests/tools/dev-cli-tests/jaribu-aggregator-summary-gate-tests.cs` — Decide / TryParse
- `tests/tools/dev-cli-tests/dev-cli-tests.csproj` — compile-include the helper
- `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`
- `skills/tw-feature-placement/SKILL.md`

**Decisions**

- Parse and decide live in a Terminal-free helper so `dev-cli-tests` can compile-include it (same pattern as `entra-setup.cs`). MTP `failed:`/`skipped:` regexes sit next to `total:`/`succeeded:` there; harness `Total:`/`Passed:` stay for tier-2 Jaribu TerminalSink.
- `failed:` absent → unparsable (fail). `skipped:` absent → 0.
- Exit code 0 is a harness precondition, not part of `Decide`.

**Gates**

- `cd tests/tools/dev-cli-tests && dotnet test -c Release` — 57 passed / 0 failed
- `dotnet run tools/dev-cli/dev.cs -- build` — 0 Warning(s) / 0 Error(s)
- `dotnet run tools/dev-cli/dev.cs -- template-smoke` — SUCCEEDED (SmokeDefault, SmokeNoPostgres, SmokeNoApi). Aggregator lines: web 180/180, api 9/9, common 3/3; SmokeNoApi correctly excludes api + common aggregators.
- `ganda repo audit` — passes (2 pre-existing advisory warnings: memsearch-scaffold, vscode-window-icon)

### How to validate

**Smoke**

```bash
cd tests/tools/dev-cli-tests && dotnet test -c Release
dotnet run tools/dev-cli/dev.cs -- build
dotnet run tools/dev-cli/dev.cs -- template-smoke
ganda repo audit
```

**Expect**

- `dev-cli-tests`: `failed: 0`, `succeeded: 57` (or more if other tests land), including Decide cases: floor exact, above floor, any failed, unparsable, >2× floor warning, total ≠ succeeded+skipped.
- `build`: `0 Warning(s) / 0 Error(s)` and `Build completed successfully!`
- `template-smoke`: `Template smoke SUCCEEDED`. Each included aggregator prints `MTP summary total=N succeeded=N skipped=0 (floor F)` then `N/N succeeded via MTP` with N ≥ F. SmokeNoApi prints `api-jaribu-tests: correctly excluded (--api false)` and the same for `timewarp-testing-tests`.
- `ganda repo audit`: `Repository passes` (advisory memsearch/vscode warnings are pre-existing and non-blocking).

**Automated gate**

```bash
cd tests/tools/dev-cli-tests && dotnet test -c Release -- --filter-class Decide_Given_
# expect: all Decide_Given_ methods passed
```

**Not in scope:** raising the floors; deriving them from a monorepo aggregator run.
