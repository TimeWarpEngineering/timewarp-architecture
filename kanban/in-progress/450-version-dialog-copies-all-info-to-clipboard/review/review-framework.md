# Review framework

## Budget (by-diff)

- Lines changed: 495
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-30
**Host task:** kanban/in-progress/450-version-dialog-copies-all-info-to-clipboard/
**Diff scope:** commit 720786e51 (`feat(web-spa): version dialog copies all info to clipboard`) vs its parent 81a9ccb35, product paths only (`git show 720786e51 -- . ':!kanban'`). 7 product files: `application-state.copy-assembly-info.cs` (new), `application-state.cs`, `application-state.set-active-modal.cs`, `AssemblyInfoCopyButton.razor` (new), `AssemblyInfoModal.razor`, `assembly-info-text.cs` (new), `tests/.../features/application/assembly-info-copy-tests.cs` (new).
**Plan / brief:** task.md `## Description` and `## Goal (implementer restatement)`: icon-only Copy button in the footer version dialog that writes every displayed AssemblyInfo row plus CommitUrl and RepositoryUrl as `Name: value` lines, via a state action through ClipboardJsModule, Warning bar on denial, Copied checkmark on success.
**Effort:** 2 (host Budget.ByDiff); roster axis general
**Reviewer roster:** general (sonnet subagent, read-only) + orchestrator verification (build, test run, call-site checks)
**Session IDs:** review oracle claude-fable-5-1 (headless `ganda task work`, print mode); general reviewer claude-sonnet-5-5 subagent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
