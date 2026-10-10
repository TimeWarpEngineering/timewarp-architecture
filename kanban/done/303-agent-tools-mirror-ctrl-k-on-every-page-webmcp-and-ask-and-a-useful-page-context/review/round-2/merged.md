# Round 2 — merged findings
**Date:** 2026-10-11
**Sources:** general (re-review of fix delta `95f184981` plus M1–M12)

## Counts

Final ledger, carrying the round-1 IDs plus new round-2 IDs:

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 1 |
| suggestion | 0 | 4 | 1 |
| nit | 0 | 4 | 2 |

## Prior findings (carried)

| ID | Severity | Status | Round-2 verification |
|----|----------|--------|----------------------|
| M1 | bug | fixed | Verified. Path keying is consistent (PathOr, RememberPageSurface, and Describe all normalize; the compare ignores case). The live walk cannot hang or throw. |
| M2 | bug | fixed | Verified. Bound drops surface text whole. Page facts are not dropped (documented). |
| M3 | bug | wontfix | Technical facts verified. The round-1 note said the follow-up was on task.md; it was not. It is now in task.md Notes. |
| M4 | suggestion | fixed | Verified. |
| M5 | suggestion | wontfix | Rationale corrected. The navigation completes, but the whole in-flight turn (prompt, navigate call, and result) is lost, not just the follow-up sentence: `ask-conversation-thread.cs` saves a turn only after streaming finishes. Still out of scope (hoist the Ask panel out of TimeWarpPage). The follow-up with the corrected wording is now in task.md Notes. |
| M6 | suggestion | fixed | Verified. |
| M7 | suggestion | fixed | Verified. |
| M8 | nit | fixed | Verified. |
| M9 | nit | fixed | Verified. Known residual: page facts are never dropped, so up to 20 long or non-ASCII filing titles could still push Ask's copy past MaxLength, and `For` would then clip it. This is documented as the last guard. Not raised as a new finding. |
| M10 | nit | wontfix | Verified. RolePage has no `Navigable = true`; the registry lists only navigable pages. |
| M11 | nit | fixed | Verified. |
| M12 | nit | wontfix | Technical facts verified. The flag for Steven is now in task.md Notes. |

## Issues

### N1 — Severity: suggestion — Status: fixed
- File: kanban/.../303-.../task.md (Results)
- Description: Results were stale. The `/Feedback` sample still showed the M1 bug (home headings), the test counts were old (generator 28 vs 29, CatalogAgent 38 vs 40), and the M3, M5, and M12 follow-ups were missing.
- Suggestion: Refresh Results and add the follow-ups.
- Source: general
- Disposition notes: Results updated: corrected `/Feedback` sample, test counts, a whole-project run (190 passed), and a Review section. Notes carry the M3, M5, and M12 follow-ups.

### N2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/services/page-surface-js-module.cs:9
- Description: The Design regions said TrySummarizeAsync returns null for "an empty walk", and page-agent-context.cs said "when that walk returns nothing". A page with no body still returns JSON with empty arrays.
- Suggestion: Describe when null is actually returned.
- Source: general
- Disposition notes: Both Design regions now say null means no runtime, a failed JS call, or an empty string, and that an empty page still returns JSON with empty arrays.

## Duplicates / conflicts

- None. The M5 rationale was corrected, not reopened: the scope decision (separate task) stands.
