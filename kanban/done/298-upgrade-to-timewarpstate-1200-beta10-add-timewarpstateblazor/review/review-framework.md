# Review framework

## Budget (by-diff)

- Lines changed: 736
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

# Review framework — task 298

**Date:** 2026-10-10
**Host task:** kanban/done/298-upgrade-to-timewarpstate-1200-beta10-add-timewarpstateblazor/
**Plan / brief:** Bump TimeWarp.State / Plus to 12.0.0-beta.10, add TimeWarp.State.Blazor, handle beta.10 breaking changes; then fix CI template-smoke `/_content/TimeWarp.State/` resolution.
**Reviewer roster:** general
**Session IDs:** Claude review oracle (ganda task work, headless)

| Round | Diff scope | Effort |
|-------|------------|--------|
| 1 | branch vs master at 53d5c0a09 (beta.10 pins, TimeWarp.State.Blazor reference, registration) | 1 |
| 2 | c6b085f0d (template-smoke content-asset resolver, dev-cli-tests) re-checked against the full branch diff | 2 (by-diff budget above) |
