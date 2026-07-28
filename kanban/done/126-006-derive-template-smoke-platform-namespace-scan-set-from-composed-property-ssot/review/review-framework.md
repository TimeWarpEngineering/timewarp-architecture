# Review framework — task 126-006

**Date:** 2026-07-27
**Host task:** kanban/in-progress/126-006-derive-template-smoke-platform-namespace-scan-set-from-composed-property-ssot/
**Diff scope:** commit `39b82b28` — `tools/dev-cli/endpoints/template-smoke-command.cs` only (vs pre-implement parent)
**Plan / brief:** Derive unsafe platform-namespace scan suffixes at runtime from `msbuild/timewarp-platform-packages.props` (Option 1). No scan-surface expansion. Hard-fail empty derivation. TypedIds from namespace property first segment.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestration/review — grok (2026-07-27)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
