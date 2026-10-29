# Review framework

## Budget (by-diff)

- Lines changed: 62
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-30
**Host task:** kanban/in-progress/450-version-dialog-copies-all-info-to-clipboard/
**Diff scope:** commit 720786e51 (`feat(web-spa): version dialog copies all info to clipboard`) vs its parent 81a9ccb35, product paths only (`git show 720786e51 -- . ':!kanban'`). 7 product files: `application-state.copy-assembly-info.cs` (new), `application-state.cs`, `application-state.set-active-modal.cs`, `AssemblyInfoCopyButton.razor` (new), `AssemblyInfoModal.razor`, `assembly-info-text.cs` (new), `tests/.../features/application/assembly-info-copy-tests.cs` (new).
**Plan / brief:** task.md `## Description` and `## Goal (implementer restatement)`: icon-only Copy button in the footer version dialog that writes every displayed AssemblyInfo row plus CommitUrl and RepositoryUrl as `Name: value` lines, via a state action through ClipboardJsModule, Warning bar on denial, Copied checkmark on success.
**Effort:** 2 (host Budget.ByDiff); roster axis general
**Reviewer roster:** general (sonnet subagent, read-only) + orchestrator verification (build, test run, call-site checks)
**Session IDs:** review oracle claude-fable-5-1 (headless `ganda task work`, print mode); general reviewer claude-sonnet-5-5 subagent

## Round 2 scope

**Date:** 2026-10-30
**Diff scope:** fix-pass commit 92906f076 (`fix(web-spa): version dialog copy review round 1 fixes`), `git diff 152c2f866..HEAD -- . ':!kanban'`, product paths only. 9 files: `agent-links-state.copy-onboarding.cs`, `application-state.copy-assembly-info.cs`, `AssemblyInfoModal.razor`, `assembly-info-text.cs`, `notification-state.copy-message.cs`, `services/clipboard-js-module.cs`, `tests/.../application/assembly-info-copy-tests.cs`, `tests/.../notification/message-bar-copy-tests.cs`, `tests/.../notification/notification-copy-tests.cs`.
**Brief:** re-verify round-1 M1–M5 against the post-fix tree and scan the fix delta for new defects. Round 1 is frozen.
**Effort:** 2 (host Budget.ByDiff, 366 lines); roster axis general
**Reviewer roster:** general (sonnet subagent, read-only) + orchestrator verification (build, test run, call-site checks)
**Session IDs:** review oracle claude-fable-5-1 (headless `ganda task work`, print mode, turn cap 120); general reviewer claude-sonnet-5-5 subagent

## Round 3 scope

**Date:** 2026-10-30
**Diff scope:** fix-pass commit 769e9eec9 (`fix(web-spa): version dialog copy review round 2 fixes`), `git diff ce654d715..HEAD -- . ':!kanban'`, product paths only. 5 files: `agent-links-state.copy-onboarding.cs`, `application-state.copy-assembly-info.cs`, `assembly-info-text.cs`, `services/clipboard-js-module.cs`, `tests/.../application/assembly-info-copy-tests.cs`.
**Brief:** re-verify round-2 M6–M8 against the post-fix tree and scan the fix delta for new defects. Rounds 1 and 2 are frozen.
**Effort:** 1 (host Budget.ByDiff, 62 lines); roster axis general
**Reviewer roster:** general (sonnet subagent, read-only) + orchestrator verification (build, test run, call-site and markup-order checks)
**Session IDs:** review oracle claude-fable-5-1 (headless `ganda task work`, print mode, turn cap 80); general reviewer claude-sonnet-5-5 subagent a5fb2dd97881b9fe8

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
