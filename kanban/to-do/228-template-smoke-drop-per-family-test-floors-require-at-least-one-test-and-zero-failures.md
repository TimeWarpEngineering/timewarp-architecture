# Template smoke: drop per-family test floors, require at least one test and zero failures

## Description

Task 226 replaced the exact aggregator count with a per-family `MinimumSucceeded` floor
(web 180, api 9, common 3) plus a "raise the floor" warning. Steve's review (2026-09-16): the
floor is still a number nobody wants to maintain; 227 already had to move it when it legitimately
removed tests. The only real failure mode the count ever guarded is the generated app silently
discovering **zero** tests (co-located files excluded by template config). Guard that directly.

## Requirements

- In `tools/dev-cli/services/jaribu-aggregator-summary-gate.cs` the decision becomes exactly:
  exit code 0 (harness precondition, unchanged) **and** `failed == 0` **and** `total > 0` **and**
  `total == succeeded + skipped`. Unparsable summary still fails.
- Remove `MinimumSucceeded` from the `JaribuFamilyAggregators` tuples in
  `tools/dev-cli/services/template-smoke-harness.cs` (keep `RequiredFamilies` and
  `RelativeProjectDir`), remove the 2× "raise the floor" warning, keep the Info line that prints
  the actual totals.
- Update `tests/tools/dev-cli-tests/jaribu-aggregator-summary-gate-tests.cs`: cases for
  zero total fails, any failed fails, mismatch total fails, unparsable fails, `1/1` passes,
  large totals pass; delete floor/warning cases.
- Update the comment block above the aggregators and
  `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` (226 documented the
  floor there) so nothing tells contributors to bump or raise a number.

## Depends on

- 227

## Checklist

- [ ] Gate = exit 0, failed 0, total > 0, total == succeeded + skipped
- [ ] Floors and warning removed from the harness; totals still logged
- [ ] Tests updated; `cd tests/tools/dev-cli-tests && dotnet test -c Release` green
- [ ] Docs/comments updated (harness comment, co-located-jaribu-runfiles.md)
- [ ] `dev build` 0/0; `ganda repo audit` clean; `dev template-smoke` passes
- [ ] Results and How to validate

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Prior: 226 (floor gate, PR #372), 227 (lowers web floor to 177 on its branch).
- Five-line change in spirit; keep it that small.

## Results

_Pending._

### How to validate

_Pending._
