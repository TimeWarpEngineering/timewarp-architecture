# Review framework

## Budget (by-diff)

- Lines changed: 192
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 365

**Date:** 2026-10-15
**Host task:** kanban/to-do/365-dev-test-runs-test-projects-with-no-build-after-the-solution-build/
**Diff scope:** branch task/365-dev-test-runs-test-projects-with-no-build-after-th vs origin/master (commit 6b6a42d1c)
**Plan / brief:** `dev test --no-build` passes `--no-build --no-restore` to .slnx-listed test projects; `dev workflow` uses it after the Release solution build; default `dev test` still builds.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
