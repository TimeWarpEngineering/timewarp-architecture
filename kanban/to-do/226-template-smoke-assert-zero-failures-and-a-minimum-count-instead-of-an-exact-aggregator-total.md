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

- [ ] Floor + zero-failure assertion; parse failed/skipped; unparsable summary fails
- [ ] `MinimumSucceeded` rename and comment; actual total logged; 2× floor warning
- [ ] Jaribu tests for the decision helper
- [ ] `dev template-smoke` passes locally (SmokeDefault / SmokeNoPostgres / SmokeNoApi)
- [ ] `dev build` 0/0; `ganda repo audit` clean
- [ ] Results and How to validate

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Evidence: PR #365 (219-006) run 34981521704 and PR #371 (225) run 35055916884, both
  "aggregator did not report N/N (exit 0; parsed total=M, succeeded=M)" with M > N and zero
  failures.
- Files: `tools/dev-cli/services/template-smoke-harness.cs` (~L566-606 and ~L840-870),
  `tests/tools/dev-cli-tests/`.

## Results

_Pending._

### How to validate

_Pending._
