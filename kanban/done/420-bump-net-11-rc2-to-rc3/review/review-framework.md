# Review framework

## Budget (by-diff)

- Lines changed: 217
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 420

**Date:** 2026-10-26
**Host task:** kanban/in-progress/420-bump-net-11-rc2-to-rc3/
**Diff scope:** branch `task/420-bump-net-11-rc2-to-rc3` vs merge-base `0c42c3c59` on master (product commit `188664149`; `Directory.Packages.props`, root `global.json`, 22 `tests/**/global.json`)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: move SDK and shared-framework pins RC2 to RC3, hold EF Core / Npgsql on RC2 if Npgsql has no RC3, update the train and OpenApi comments, check the RC3 notes, run the gates
**Effort:** 2 (by-diff budget above)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session id not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
