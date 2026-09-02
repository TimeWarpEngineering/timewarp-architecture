# Review framework — task 209

**Date:** 2026-09-02
**Host task:** kanban/to-do/209-aspire-135-update-changelog-impact-and-withterminal-vs-nuru/
**Diff scope:** branch `task/209-aspire-135-update-changelog-impact-and-withtermina` vs `master` (commit b19ee730; `git diff master...HEAD`)
**Plan / brief:** Aspire 13.5.2 → 13.5.3 one-train bump (AppHost SDK + CPM hosting/testing/EF-preview pins), vendored `aspire*` / `dotnet-inspect` skill reconciliation with the 13.5 CLI (`aspire ps --include-hidden` removed, VS Code auto-launch removed), `.gitignore` for memsearch daily notes, task.md Results (breaking-change table, WithTerminal vs Nuru verdict, cross-repo scan).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle — claude (Fable 5.1) under `ganda task work 209`; reviewer sub-agent — claude sonnet

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
