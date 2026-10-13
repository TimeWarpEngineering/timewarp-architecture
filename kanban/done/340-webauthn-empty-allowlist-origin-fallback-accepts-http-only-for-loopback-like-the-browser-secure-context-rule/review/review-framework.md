# Review framework

## Budget (by-diff)

- Lines changed: 287
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 340

**Date:** 2026-10-13
**Host task:** kanban/to-do/340-webauthn-empty-allowlist-origin-fallback-accepts-http-only-for-loopback-like-the-browser-secure-context-rule/
**Diff scope:** branch `task/340-…` vs `origin/master` (commit d52b67d39)
**Plan / brief:** task.md Requirements — loopback-only http in the WebAuthn empty-Origins fallback, one shared loopback predicate, Design regions, tests, tw-deploy note.
**Effort:** 2 (by-diff budget), roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
