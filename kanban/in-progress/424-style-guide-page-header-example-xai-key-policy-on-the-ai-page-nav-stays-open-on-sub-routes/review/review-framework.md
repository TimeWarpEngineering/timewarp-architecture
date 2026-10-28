# Review framework

## Budget (by-diff)

- Lines changed: 577
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 3

**Date:** 2026-10-28
**Diff scope:** the fix-pass delta since round 2, `31bdd4ae7..0057d58aa` (product commit `727527b21`),
excluding `kanban/`.
**Plan / brief:** the open round-2 findings M20, M23, M24, and M25 (`review/round-2/merged.md`), and
task.md `## Goal (implementer restatement)`.
**Effort:** 2 (by-diff budget above). Roster: general. The delta is 130 lines added and 18 removed,
so the oracle reviewed it inline as the single general reviewer (`review/round-3/general.md`).
**Session IDs:** review oracle (claude-opus-5-5, headless `ganda task work`). No subagents.

## Round 2

**Date:** 2026-10-28
**Host task:** kanban/in-progress/424-style-guide-page-header-example-xai-key-policy-on-the-ai-page-nav-stays-open-on-sub-routes/
**Diff scope:** the fix-pass delta since round 1, `0d475995b..31bdd4ae7` (product commit `0c0c617af`),
excluding `kanban/`.
**Plan / brief:** `review/round-1/merged.md` M1–M19, and task.md `## Goal (implementer restatement)`.
**Effort:** 3. Three general reviewers on the same slices as round 1.
**Reviewer roster:**
- general-1: xAI key policy (M1, M2, M4, M5, M14, M15)
- general-2: nav match, registry, palette (M6–M9, M13, M16, M18)
- general-3: page header, CSS, skills (M3, M10–M12, M17, M19)

**Session IDs:** review oracle (claude-opus-5-5, headless `ganda task work`); reviewer subagents
(sonnet) a931bee9961c5891c, af53d92a36f8141f0, a08f3135265eb010c.

Round 1's framework header (scope `0d475995b` vs `aea778347`) was replaced when the host rewrote this
file for the round 2 budget. The round 1 scope is still in `review/round-1/merged.md` and task.md
`## Session`.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
