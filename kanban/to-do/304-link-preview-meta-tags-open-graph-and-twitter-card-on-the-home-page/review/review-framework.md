# Review framework

## Budget (by-diff)

- Lines changed: 827
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 304

**Date:** 2026-10-11
**Host task:** kanban/to-do/304-link-preview-meta-tags-open-graph-and-twitter-card-on-the-home-page/
**Diff scope:** branch `task/304-link-preview-meta-tags-open-graph-and-twitter-card` vs `master` (commit dd79d5a17)
**Plan / brief:** task.md Requirements 1–7: server-rendered OG / Twitter card / canonical tags in the
static `App.razor` head, `SiteMetadata` options, 1200×630 PNG, integration test against `GET /`.
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** Claude Code review oracle (Opus 5.5), 2026-10-11

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space. Zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
