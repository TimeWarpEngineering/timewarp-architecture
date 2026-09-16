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

- [x] Gate = exit 0, failed 0, total > 0, total == succeeded + skipped
- [x] Floors and warning removed from the harness; totals still logged
- [x] Tests updated; `cd tests/tools/dev-cli-tests && dotnet test -c Release` green
- [x] Docs/comments updated (harness comment, co-located-jaribu-runfiles.md)
- [x] `dev build` 0/0; `ganda repo audit` clean; `dev template-smoke` passes
- [x] Results and How to validate
- [x] Implementation review disposition (clean)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: grok session 01a0aa0f-9746-7750-b4d8-106e935cc6d0 (2026-09-16)
- Review oracle: grok session 01a0aa1a-4b39-7432-9276-a8fa6059416a (2026-09-16)
- Reviewer (round 1 general): grok session 01a0aa1c-714b-75d1-8261-84aad6f3c2aa (2026-09-16)

## Notes

- Prior: 226 (floor gate, PR #372), 227 (lowers web floor to 177 on its branch).
- Five-line change in spirit; keep it that small.

## Results

Template-smoke tier 3 no longer maintains a per-family `MinimumSucceeded` floor.
The generated Jaribu aggregators must exit 0, report `failed == 0` (`failed:`
required), `total > 0`, and `total == succeeded + skipped` (missing `skipped:`
is 0). An unparsable summary still fails. That is the silent zero-discovery
guard; adding or removing co-located tests no longer requires bumping a number.
The smoke log still prints the actual MTP totals.

**Files**

- `tools/dev-cli/services/jaribu-aggregator-summary-gate.cs` — `Decide` is
  failed==0 / total>0 / total==succeeded+skipped; dropped `minimumSucceeded`,
  `FailBelowFloor`, and `WarnStaleFloor`
- `tools/dev-cli/services/template-smoke-harness.cs` — `JaribuFamilyAggregators`
  is `(RequiredFamilies, RelativeProjectDir)` only; 2× warning removed; totals
  Info line kept
- `tests/tools/dev-cli-tests/jaribu-aggregator-summary-gate-tests.cs` — zero
  total, any failed, mismatch, unparsable, `1/1`, large totals; floor/warning
  cases deleted
- `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`
- `skills/tw-feature-placement/SKILL.md` — dropped the floor bump instruction

**Decisions**

- `FailZeroTotal` is a dedicated verdict (`total == 0`) so silent
  zero-discovery is distinct from a parse failure.
- Exit code 0 stays a harness precondition, not part of `Decide`.
- SKILL.md was updated alongside the reference so no contributor-facing
  surface still says to raise a number.

**Gates**

- `cd tests/tools/dev-cli-tests && dotnet test -c Release` — 55 passed / 0 failed
- `dotnet run tools/dev-cli/dev.cs -- build` — 0 Warning(s) / 0 Error(s)
- `./bin/dev template-smoke` — SUCCEEDED (SmokeDefault, SmokeNoPostgres,
  SmokeNoApi). Aggregator lines: web 177/177, api 9/9, common 3/3; SmokeNoApi
  correctly excludes api + common aggregators. Totals logged without a floor.
- `ganda repo audit` — passes (2 pre-existing advisory warnings:
  memsearch-scaffold, vscode-window-icon)

### How to validate

**Smoke**

```bash
cd tests/tools/dev-cli-tests && dotnet test -c Release
dotnet run tools/dev-cli/dev.cs -- build
dotnet run tools/dev-cli/dev.cs -- template-smoke
ganda repo audit
```

**Expect**

- `dev-cli-tests`: `failed: 0`, `succeeded: 55` (or more if other tests land),
  including Decide cases: zero total fails, any failed fails, mismatch fails,
  unparsable fails, `1/1` passes, large totals pass. No floor/warning cases.
- `build`: `0 Warning(s) / 0 Error(s)` and `Build completed successfully!`
- `template-smoke`: `Template smoke SUCCEEDED`. Each included aggregator prints
  `MTP summary total=N succeeded=N skipped=0` then `N/N succeeded via MTP` with
  N > 0. No `(floor …)` in those lines. SmokeNoApi prints
  `api-jaribu-tests: correctly excluded (--api false)` and the same for
  `timewarp-testing-tests`.
- `ganda repo audit`: `Repository passes` (advisory memsearch/vscode warnings
  are pre-existing and non-blocking).

**Automated gate**

```bash
cd tests/tools/dev-cli-tests && dotnet test -c Release -- --filter-class Decide_Given_
# expect: all Decide_Given_ methods passed
```

**Not in scope:** restoring a per-family succeeded floor; deriving a count from
a monorepo aggregator run.

### Review disposition

- **Outcome:** clean
- **Rounds:** 1
- **Effort / roster:** 1, general only
- **Final counts:** bug 0/0/0, suggestion 0/0/0, nit 0/0/0 (open/fixed/wontfix)
- **Wontfix / escalations:** none
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/disposition.md`
