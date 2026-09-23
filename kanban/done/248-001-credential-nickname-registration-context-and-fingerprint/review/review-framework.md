# Review framework — task 248-001

**Date:** 2026-09-23
**Host task:** kanban/to-do/248-001-credential-nickname-registration-context-and-fingerprint/
**Diff scope:** branch `task/248-001-credential-nickname-registration-context-and-finge` vs merge-base `e3bb24b7` (origin/master); commits `4ff46167`, `e8428537` — 55 files
**Plan / brief:** task.md Requirements — credential `Nickname` + `RenameCredential` endpoint, registration context (`RegisteredWith`: attachment/browser/OS, raw UA never stored), `Fingerprint` on `GetCredentials.CredentialSummary` (no material on the wire), `CredentialList` row + inline rename + restating revoke confirmation, co-located Jaribu + integration + SPA presenter tests, EF mapping + postgres migration.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle — ganda task work (Claude Fable 5.1), 2026-09-23; reviewer subagent spawned from that session

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 — re-review after the merge-master fix loop (2026-09-23)

**Trigger:** after round-1 disposition, PR #397 conflicted with master (task 246 merged via
PR #396: `Delete*` → `Revoke*` parameters on `CredentialList`, `CanRevoke` last-credential
guard + hint on Settings/Passkeys). The fix loop merged `origin/master` into this branch
(merge commit `3aeed9b1`, no rebase/squash) and hand-resolved four conflicts.
**Diff scope:** the merge resolution — conflict hunks of `3aeed9b1` (`git show --cc 3aeed9b1`)
in `SettingsPage.razor`, `SettingsPage.razor.cs`, `CredentialList.razor`, `PasskeysPage.razor`,
`credential-list-render-tests.cs`, `credentials-spa-test-application.cs`; plus the branch delta
vs `origin/master` (`git diff origin/master...HEAD`) for those files and their call sites.
**Brief:** task.md "Fix loop (2026-09-23, cockpit)" — BOTH must land: 246's Revoke naming and
last-credential disable + hint, AND this task's row (nickname title, context line, fingerprint,
inline rename, two-step restating confirmation); the confirm button raises `OnRevoke`; the 246
disabled state also disables the first step; 246's tests stay green alongside
`credential-list-render-tests.cs`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle — ganda task work (Claude Fable 5.1), 2026-09-23; reviewer subagent spawned from that session
**Carry-forward:** M1–M7 from `round-1/merged.md` are re-verified against the post-merge tree; new findings get M8+.
