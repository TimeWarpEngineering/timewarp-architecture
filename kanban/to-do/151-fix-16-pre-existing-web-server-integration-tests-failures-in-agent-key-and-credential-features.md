# Fix 16 pre-existing web-server-integration-tests failures in agent-key and credential features

## Description

Discovered during task 150 (2026-08-05): `tests/container-apps/web/web-server-integration-tests`
has 16 failing tests at dev tip `c8ae9def` — before and independent of the task-150 diff
(verified by running the suite with all 150 changes stashed: identical 16 failures both ways).
Suite totals at `6bd81f13`: 112 total / 16 failed / 95 passed / 1 skipped.

All 16 are in agent-key / credential-management features. Names observed include
`ValidationError_Given_Empty_Name`, `Conflict_Given_Duplicate_Key`,
`Forbidden_Given_Quarantined_Principal` (full list falls out of the repro command below).

Unknown when they started failing — possibly since a recent auth/identity change (e.g.
`55ee9384` or the task 148/149 work) or an earlier regression that CI did not gate (this suite
runs via `dev test`, not the solution build). Root-causing when/why they broke is part of the
task.

## Requirements

- Bisect or otherwise identify the commit/change that introduced the failures.
- Fix product code or tests, whichever is actually wrong — do not blanket-skip.
- Full suite green: `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release`
  ends 0 failed (the 1 pre-existing skip may remain if intentional).
- Note whether CI should have caught this and, if so, what gate is missing (follow-up task if
  non-trivial).

## Checklist

- [ ] Reproduce: run the suite at dev tip; capture the full list of 16 failing test names and
      their failure messages
- [ ] Identify the introducing commit (git bisect over the suite or targeted class filters)
- [ ] Root-cause each failure cluster (likely one shared cause across agent-key/credential
      features)
- [ ] Fix; suite fully green
- [ ] Assess CI gate coverage for this suite; file follow-up if a gap exists
- [ ] Results with How to validate

## Notes

- Discovered/documented in task 150 Results ("Known issue out of scope"); the reviewer there
  confirmed the failures are confined to non-profile features.
- Fixed-port suite (web=7000, api=7255) — run serialized, no parallel test runs.

## Session

- Created: Claude (2026-08-05, during task 150 orchestration)
