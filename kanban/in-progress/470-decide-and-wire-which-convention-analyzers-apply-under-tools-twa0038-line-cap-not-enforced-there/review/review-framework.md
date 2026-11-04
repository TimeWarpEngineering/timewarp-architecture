# Review framework

## Budget (by-diff)

- Lines changed: 150
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 470

**Date:** 2026-11-04
**Host task:** kanban/in-progress/470-decide-and-wire-which-convention-analyzers-apply-under-tools-twa0038-line-cap-not-enforced-there/
**Diff scope:** branch `task/470-decide-and-wire-which-convention-analyzers-apply-u` vs `master` (ee4993ce8 product commit; c83a4bf12 kanban land). Product files: `tools/Directory.Build.props` (new), `.editorconfig`, `source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs`.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Notes` decision (Steve 2026-11-04): full convention set under tools/; TWA0008 off for template-smoke via .editorconfig; NuGet package content exempt by path; no other exemptions; decision in a Design region.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), ganda task work review node 2026-11-04

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
