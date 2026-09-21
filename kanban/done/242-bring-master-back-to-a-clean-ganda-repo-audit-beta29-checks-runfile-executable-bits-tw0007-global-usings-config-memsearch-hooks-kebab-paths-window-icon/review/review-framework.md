# Review framework — task 242

**Date:** 2026-09-21
**Host task:** kanban/in-progress/242-bring-master-back-to-a-clean-ganda-repo-audit-beta29-checks-runfile-executable-bits-tw0007-global-usings-config-memsearch-hooks-kebab-paths-window-icon/
**Diff scope:** branch `task/242-bring-master-back-to-a-clean-ganda-repo-audit-beta` vs `origin/master` (commit `8d5c494d`)
**Plan / brief:** Restore a clean `ganda repo audit` (ganda 1.0.0-beta.29) on architecture master: runfile +x, house shebang, TW0007 editorconfig + SourceGenerators pin, memsearch hooks via fixer, peacock.color, and a CI `repo-audit` job as the guard so new shebang runfiles cannot land without the executable bit.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok task-work review oracle 2026-09-21

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

**Date:** 2026-09-21
**Scope:** Re-verify M1 (fixed: github-only TimeWarp.Ganda install + nuget.org-vintage version gate) and M2 (wontfix: kanban/** omitted from workflow path filters). Scan the workflow.yml fix delta for new defects. Carry stable M# IDs.

