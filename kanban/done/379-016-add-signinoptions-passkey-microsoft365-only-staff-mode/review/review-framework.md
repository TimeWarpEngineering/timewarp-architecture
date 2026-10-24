# Review framework

## Budget (by-diff)

- Lines changed: 316
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 379-016

**Date:** 2026-10-24
**Host task:** kanban/to-do/379-016-add-signinoptions-passkey-microsoft365-only-staff-mode/
**Diff scope:** branch `task/379-016-add-signinoptions-passkey-microsoft365-only-staff` vs `master` — commit 6861cc5bd (source + tests)
**Plan / brief:** `## Goal (implementer restatement)` and `## Description` in task.md: `SignInOptions.Mode` (Passkey / Microsoft365Only / Staff) drives the login card, the add-passkey prompt and required gate, and the choose-account page.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
