# Review framework

## Budget (by-diff)

- Lines changed: 4446
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 279

**Date:** 2026-10-05
**Host task:** kanban/to-do/279-adopt-hypermedia-approach-b-on-the-real-credentials-pages-remove-the-lab-and-approach-c/
**Diff scope:** branch `task/279-adopt-hypermedia-approach-b-on-the-real-credential` vs `master` (`git diff master...HEAD`; commits 628aedca2..0de7ef446)
**Plan / brief:** task.md Requirements 1–7 + Decisions — server offers on `GetCredentials` from `CredentialRules`; SPA runs offers through the action catalog with an M4 Visibility/Permissions gate; Ctrl-K contextual rows; lab + approach C removed; tw-blazor skill section.
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general, tests, security (parallel; each read-only)
**Session IDs:** review oracle — headless Claude Opus 5.5 (ganda task work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
