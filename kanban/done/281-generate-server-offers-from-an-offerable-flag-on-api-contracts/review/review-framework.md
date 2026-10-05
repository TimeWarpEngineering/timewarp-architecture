# Review framework

## Budget (by-diff)

- Lines changed: 1452
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 281

**Date:** 2026-10-06
**Host task:** kanban/to-do/281-generate-server-offers-from-an-offerable-flag-on-api-contracts/
**Diff scope:** branch `task/281-generate-server-offers-from-an-offerable-flag-on-a` vs `master` (commit 4607ddb3b)
**Plan / brief:** task.md Requirements 1–6 — `[Offerable]` attribute, contracts generator emits `Offer` + `OfferName` + `[ActionOffer]`, TWE012/TWE013, TWA0031 contract→client action link, Revoke/Rename migration, LinkMicrosoft365 escape hatch, skills.
**Effort:** 3 (by-diff budget) — roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (claude, headless `ganda task work`); general reviewer = Claude subagent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
