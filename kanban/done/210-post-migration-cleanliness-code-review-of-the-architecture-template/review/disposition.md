# Disposition — task 210

**Date:** 2026-09-12
**Outcome:** clean
**Rounds:** 1 (repo-wide, six specialist reviewers + orchestrator), followed by six child fix tasks each with its own review loop
**Final open count:** 0

## Summary

Round 1 produced 41 merged findings (18 bug, 15 suggestion, 8 nit by final heading severity).
M1 was fixed on the review PR itself; M17 on this closeout; the other 39 were dispatched as
children 210-001 … 210-006 and every one landed as `fixed` — no `wontfix` at the parent level.
Two human decisions were taken during the loop and are recorded on the children: `documentation/`
is retired and skills ship in the template (M2, 210-005); the authorization engine and payment
port move to `platform/` while permission ids stay Features substrate (M8, 210-002).

| Child | PR | Findings |
|-------|----|----------|
| 210-001 dead files + stale TODOs | #345 | M11 M16 M31 M35 M37 M38 M39 M40 M41 |
| 210-002 features/platform boundary | #347 | M3 M4 M5 M6 M7 M8 M9 M10 |
| 210-003 missing endpoint tests | #346 | M12 M13 M14 M15 |
| 210-004 agent-facing docs | #344 | M18 M19 M26 M29 |
| 210-005 retire documentation, ship skills | #349 | M2 M20 M21 M22 M23 M24 M25 M27 M28 |
| 210-006 suppressions, verify-samples, CORS | #348 | M30 M32 M33 M34 M36 |

## Exception log

None at the parent level. Child 210-005 recorded one `wontfix` in its own review: the
`ganda repo audit` `directory-structure` check still lists `documentation/` as required, so the
repo carries an `.editorconfig` severity override until ganda drops the requirement.

## Escalations

- M2 ship-scope and M8 placement: decided by Steve, 2026-09-12 (recorded on 210-005 / 210-002).
- Follow-ups outside this task: task 211 (profile-menu transitions, from M36); a ganda task to
  drop `documentation/` from `RequiredDirectories` (from 210-005's wontfix).
