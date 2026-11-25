# Review framework — task 458-001

**Date:** 2026-11-26
**Host task:** kanban/in-progress/458-001-publish-session-cookie-and-per-operation-security-requirements-in-open-api/
**Diff scope:** branch `task/458-001-publish-session-cookie-and-per-operation-security` vs `master` (ahead 3). Product diff is the OpenAPI session-cookie scheme, per-operation `security`, and the document tests. Kanban move is in the same range. `review/` is untracked kitchen output from this round.
**Plan / brief:** Publish `IdentitySessionCookie` and write each operation's `security` from `AuthSchemeNames` (session, bearer with scopes, both as alternatives, or `[]` for an anonymous verb). Document test: every operation has a `security` key, and every operation that documents `401` has a non-empty requirement except the Entra challenge.
**Effort:** 3 (by-diff, 1354 lines). Roster is general only.
**Reviewer roster:** general
**Session IDs:** grok 01a21453-33e4-7922-8c4a-7099963f9482 (review oracle, general)

## Budget (by-diff)

- Lines changed: 1354
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
