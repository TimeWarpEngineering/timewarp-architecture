# Review framework

## Budget (by-diff)

- Lines changed: 175
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 — task 470

**Date:** 2026-11-04
**Host task:** kanban/in-progress/470-decide-and-wire-which-convention-analyzers-apply-under-tools-twa0038-line-cap-not-enforced-there/
**Diff scope:** fix delta since round 1: `c83a4bf12..82383c9f0` (round-1 base.sha c83a4bf12). Product files: root `Directory.Build.props`, `source/`, `tests/`, `tools/Directory.Build.props`, `tools/dev-cli/Directory.Build.props`, `.editorconfig`, `file-length-analyzer.cs`, `source-name-literal-scan-sets.cs`.
**Plan / brief:** round-1 `merged.md` M1–M4 dispositions; task.md `## Goal (implementer restatement)`.
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review-2 oracle (claude, claude-opus-5-5), ganda task work review node 2026-11-04

## Round 3 — task 470

**Date:** 2026-11-04
**Host task:** kanban/in-progress/470-decide-and-wire-which-convention-analyzers-apply-under-tools-twa0038-line-cap-not-enforced-there/
**Diff scope:** fix delta since round 2: `82383c9f0..e64099c4b` (round-2 base.sha 82383c9f0). Product files: `.editorconfig`, `tools/Directory.Build.props`, `tools/dev-cli/services/template-smoke/source-name-literal-scan-sets.cs`.
**Plan / brief:** round-2 `merged.md` M1 disposition; task.md `## Goal (implementer restatement)`.
**Effort:** 1 (by-diff budget, 175 lines); roster axes: general
**Reviewer roster:** general
**Session IDs:** review-3 oracle (claude, claude-opus-5-5), ganda task work review node 2026-11-04
